using System.Net;
using System.Security.Cryptography;
using System.Text;
using KnowledgeApp.Application.Abstractions;
using KnowledgeApp.Infrastructure.Options;
using KnowledgeApp.Infrastructure.Services;
using Microsoft.Extensions.Options;

namespace KnowledgeApp.UnitTests.Infrastructure;

public sealed class EmbeddingModelStoreTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "localmind-model-store-tests", Guid.NewGuid().ToString("N"));

    [Fact]
    public async Task IsValidAsync_Should_Return_True_When_File_Checksum_Matches()
    {
        byte[] content = Encoding.UTF8.GetBytes("model-bytes");
        EmbeddingModelManifest manifest = CreateManifest(content);
        EmbeddingModelStore store = CreateStore(content);
        Directory.CreateDirectory(store.ModelsDirectory);
        await File.WriteAllBytesAsync(store.GetModelPath(manifest), content);

        bool isValid = await store.IsValidAsync(manifest);

        Assert.True(isValid);
    }

    [Fact]
    public async Task EnsureDownloadedAsync_Should_Download_And_Validate_Model()
    {
        byte[] content = Encoding.UTF8.GetBytes("downloaded-model-bytes");
        EmbeddingModelManifest manifest = CreateManifest(content);
        EmbeddingModelStore store = CreateStore(content);

        string modelPath = await store.EnsureDownloadedAsync(manifest);

        Assert.True(File.Exists(modelPath));
        Assert.Equal(content, await File.ReadAllBytesAsync(modelPath));
    }

    [Fact]
    public async Task EnsureDownloadedAsync_Should_Reject_Checksum_Mismatch()
    {
        byte[] content = Encoding.UTF8.GetBytes("downloaded-model-bytes");
        EmbeddingModelManifest manifest = CreateManifest(Encoding.UTF8.GetBytes("different"));
        EmbeddingModelStore store = CreateStore(content);

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(() => store.EnsureDownloadedAsync(manifest));

        Assert.Contains("checksum mismatch", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.False(File.Exists(store.GetModelPath(manifest)));
    }

    public void Dispose()
    {
        if (Directory.Exists(root))
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private EmbeddingModelStore CreateStore(byte[] responseBytes)
    {
        AiOptions options = new()
        {
            ModelsPath = "runtime/ai/models",
        };

        return new EmbeddingModelStore(
            new TestPathProvider(root),
            Options.Create(options),
            new EmbeddingModelCatalog(Options.Create(options)),
            new HttpClient(new StaticContentHandler(responseBytes)));
    }

    private static EmbeddingModelManifest CreateManifest(byte[] expectedContent)
    {
        string sha256 = Convert.ToHexString(SHA256.HashData(expectedContent)).ToLowerInvariant();
        return new EmbeddingModelManifest(
            Id: "test-model",
            ModelName: "test-model",
            DisplayName: "Test Model",
            Format: "gguf",
            Quantization: "Q4_K_M",
            Dimension: 3,
            ContextSize: 16,
            FileName: "test-model.gguf",
            SourceUrl: "https://example.test/test-model.gguf",
            SourceRepository: "example/test",
            SourceRevision: "main",
            Sha256: sha256,
            SizeBytes: expectedContent.Length,
            License: "MIT");
    }

    private sealed class TestPathProvider : IAppPathProvider
    {
        private readonly string root;

        public TestPathProvider(string root)
        {
            this.root = root;
            AppRootDirectory = root;
        }

        public string AppRootDirectory { get; }
        public string DataDirectory => Path.Combine(root, "runtime", "app", "data");
        public string DatabasePath => Path.Combine(DataDirectory, "knowledge-app.db");
        public string FilesDirectory => Path.Combine(root, "runtime", "app", "files");
        public string IndexDirectory => Path.Combine(root, "runtime", "app", "indexes");
        public string LogsDirectory => Path.Combine(root, "runtime", "app", "logs");
    }

    private sealed class StaticContentHandler(byte[] responseBytes) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            HttpResponseMessage response = new(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(responseBytes),
            };

            return Task.FromResult(response);
        }
    }
}
