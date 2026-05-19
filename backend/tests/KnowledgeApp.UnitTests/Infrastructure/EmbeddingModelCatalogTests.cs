using KnowledgeApp.Infrastructure.Options;
using KnowledgeApp.Infrastructure.Services;
using Microsoft.Extensions.Options;

namespace KnowledgeApp.UnitTests.Infrastructure;

public sealed class EmbeddingModelCatalogTests
{
    [Fact]
    public void GetDefault_Should_Load_BgeM3Manifest()
    {
        EmbeddingModelCatalog catalog = new(Options.Create(new AiOptions()));

        EmbeddingModelManifest manifest = catalog.GetDefault();

        Assert.Equal("bge-m3-q4-k-m", manifest.Id);
        Assert.Equal("bge-m3", manifest.ModelName);
        Assert.Equal("gguf", manifest.Format);
        Assert.Equal("Q4_K_M", manifest.Quantization);
        Assert.Equal(1024, manifest.Dimension);
        Assert.Equal(8192, manifest.ContextSize);
        Assert.Equal("bge-m3-Q4_K_M.gguf", manifest.FileName);
        Assert.Equal("6d39681b26c61279ac1f82db35a04a05009e94c415b51c858ff571489a82fc06", manifest.Sha256);
    }
}
