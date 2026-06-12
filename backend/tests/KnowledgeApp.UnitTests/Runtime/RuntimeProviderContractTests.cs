using KnowledgeApp.Application.Abstractions;
using KnowledgeApp.Application.Common.Errors;
using KnowledgeApp.Application.Exceptions;
using KnowledgeApp.Contracts.Runtime;
using KnowledgeApp.Infrastructure.Options;
using KnowledgeApp.Infrastructure.Services;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace KnowledgeApp.UnitTests.Runtime;

public sealed class RuntimeProviderContractTests
{
    [Fact]
    public async Task AiRuntimeManager_Should_Advertise_LlamaCpp_Provider_Capabilities()
    {
        using TemporaryRuntimePaths paths = new();

        IOptions<RuntimeOptions> runtimeOptions = Options.Create(new RuntimeOptions
        {
            RuntimePath = "missing.exe",
            BaseUrl = "http://127.0.0.1:65531",
        });

        IOptions<EmbeddingOptions> embeddingOptions = Options.Create(new EmbeddingOptions
        {
            ModelsPath = paths.ModelsPath,
        });

        EmbeddingModelCatalog catalog = new(embeddingOptions);
        ChatModelCatalog chatCatalog = new(runtimeOptions);

        AiRuntimeManager manager = new(
            paths,
            runtimeOptions,
            embeddingOptions,
            catalog,
            new EmbeddingModelStore(
                paths,
                embeddingOptions,
                catalog,
                new HttpClient()),
            chatCatalog,
            new ChatModelStore(
                paths,
                embeddingOptions,
                chatCatalog,
                new HttpClient()),
            new KnowledgeApp.Infrastructure.Services.Runtime.RuntimeProcessManager(new FakeApplicationLifetime(), new LoggerFactory().CreateLogger<KnowledgeApp.Infrastructure.Services.Runtime.RuntimeProcessManager>()),
            NullLogger<AiRuntimeManager>.Instance);

        RuntimeStatusDto status = await manager.GetStatusAsync();

        Assert.IsAssignableFrom<IAiRuntimeProvider>(manager);
        Assert.Equal("llama-cpp", status.ProviderId);
        Assert.Equal("llama.cpp", status.ProviderName);
        Assert.NotNull(status.Capabilities);
        Assert.True(status.Capabilities.SupportsEmbeddings);
        Assert.True(status.Capabilities.SupportsStart);
    }

    [Fact]
    public void ChatModelCatalog_Should_Load_Llama32_Manifest()
    {
        ChatModelCatalog catalog = new(Options.Create(new RuntimeOptions()));

        ChatModelManifest manifest = catalog.GetDefault();

        Assert.Equal("llama-3.2-3b-instruct-q4-k-m", manifest.Id);
        Assert.Equal("meta-llama/Llama-3.2-3B-Instruct", manifest.ModelName);
        Assert.Equal("Q4_K_M", manifest.Quantization);
        Assert.Equal("bartowski/Llama-3.2-3B-Instruct-GGUF", manifest.SourceRepository);
    }

    [Fact]
    public void AiRuntimeProviderRegistry_Should_Select_Configured_Provider()
    {
        FakeRuntimeProvider selected = new("stub", "Stub");
        FakeRuntimeProvider other = new("llama-cpp", "llama.cpp");

        AiRuntimeProviderRegistry registry = new(
            [other, selected],
            Options.Create(new RuntimeOptions
            {
                Provider = "stub",
            }));

        IAiRuntimeProvider provider = registry.GetSelectedProvider();

        Assert.Same(selected, provider);
    }

    [Fact]
    public void AiRuntimeProviderRegistry_Should_Return_Stable_Error_For_Missing_Provider()
    {
        AiRuntimeProviderRegistry registry = new(
            [new FakeRuntimeProvider("stub", "Stub")],
            Options.Create(new RuntimeOptions
            {
                Provider = "missing",
            }));

        ExternalDependencyAppException exception =
            Assert.Throws<ExternalDependencyAppException>(
                registry.GetSelectedProvider);

        Assert.Equal(ErrorCodes.Runtime.AiProviderNotFound, exception.Code);
    }

    private sealed class TemporaryRuntimePaths : IAppPathProvider, IDisposable
    {
        private readonly string root = Path.Combine(
            Path.GetTempPath(),
            $"localmind-runtime-test-{Guid.NewGuid():N}");

        public TemporaryRuntimePaths()
        {
            Directory.CreateDirectory(root);
            Directory.CreateDirectory(ModelsPath);
        }

        public string AppRootDirectory => root;

        public string DataDirectory => Path.Combine(root, "data");

        public string DatabasePath => Path.Combine(DataDirectory, "app.db");

        public string FilesDirectory => Path.Combine(root, "files");

        public string IndexDirectory => Path.Combine(root, "index");

        public string LogsDirectory => Path.Combine(root, "logs");

        public string ModelsPath => Path.Combine(root, "models");

        public void Dispose()
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    private sealed class FakeRuntimeProvider(string id, string name) : IAiRuntimeProvider
    {
        public string ProviderId => id;

        public string ProviderName => name;

        public string EmbeddingModelName => "test-embedding-model";

        public AiRuntimeProviderCapabilities Capabilities { get; } = new(
            SupportsEmbeddings: true,
            SupportsChat: true,
            SupportsModelListing: true,
            SupportsSetup: false,
            SupportsStart: false,
            SupportsStop: false);

        public Task<RuntimeStatusDto> GetStatusAsync(
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new RuntimeStatusDto(
                LocalApiReady: true,
                AiRuntimeStatus: "Running",
                ModelsAvailable: true,
                OfflineMode: true,
                ProviderId: ProviderId,
                ProviderName: ProviderName,
                ProviderStatus: AiRuntimeProviderStatus.Running,
                Capabilities: Capabilities));
        }

        public Task StartAsync(CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task<IReadOnlyCollection<string>> ListModelsAsync(
            CancellationToken cancellationToken = default)
        {
            IReadOnlyCollection<string> models = ["test-model"];

            return Task.FromResult(models);
        }

        public Task<string> GenerateChatCompletionAsync(
            ChatModelRequest request,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult("answer");
        }

        public async IAsyncEnumerable<string> GenerateChatCompletionStreamAsync(
            ChatModelRequest request,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            yield return "answer";
            await Task.Yield();
        }

        public Task<float[]> GenerateEmbeddingAsync(
            string text,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<float[]>([1, 0, 0]);
        }
    }
}
