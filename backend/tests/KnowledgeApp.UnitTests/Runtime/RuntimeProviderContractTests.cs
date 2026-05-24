using KnowledgeApp.Application.Abstractions;
using KnowledgeApp.Contracts.Runtime;
using KnowledgeApp.Infrastructure.Options;
using KnowledgeApp.Infrastructure.Services;
using KnowledgeApp.UnitTests;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace KnowledgeApp.UnitTests.Runtime;

public sealed class RuntimeProviderContractTests
{
    [Fact]
    public async Task AiRuntimeManager_Should_Advertise_LlamaCpp_Provider_Capabilities()
    {
        using TemporaryRuntimePaths paths = new();
        IOptions<AiOptions> options = Options.Create(new AiOptions
        {
            RuntimePath = "missing.exe",
            ModelsPath = paths.ModelsPath,
            BaseUrl = "http://127.0.0.1:65531",
        });
        EmbeddingModelCatalog catalog = new(options);
        AiRuntimeManager manager = new(
            paths,
            options,
            catalog,
            new EmbeddingModelStore(
                paths,
                options,
                catalog,
                new HttpClient()),
            NullLogger<AiRuntimeManager>.Instance);

        RuntimeStatusDto status = await manager.GetStatusAsync();

        Assert.IsAssignableFrom<IAiRuntimeProvider>(manager);
        Assert.Equal("llama-cpp", status.ProviderId);
        Assert.Equal("llama.cpp", status.ProviderName);
        Assert.NotNull(status.Capabilities);
        Assert.True(status.Capabilities.SupportsEmbeddings);
        Assert.True(status.Capabilities.SupportsStart);
    }

    private sealed class TemporaryRuntimePaths : IAppPathProvider, IDisposable
    {
        private readonly string root = Path.Combine(Path.GetTempPath(), $"localmind-runtime-test-{Guid.NewGuid():N}");

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
}
