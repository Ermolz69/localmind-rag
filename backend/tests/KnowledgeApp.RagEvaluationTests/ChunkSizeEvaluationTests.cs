using KnowledgeApp.Infrastructure.Options;
using KnowledgeApp.Infrastructure.Services;
using Microsoft.Extensions.Options;
using Xunit;

namespace KnowledgeApp.RagEvaluationTests;

public class ChunkSizeEvaluationTests
{
    [Fact]
    public void Document_Chunk_Size_Evaluation_Plan()
    {
        // This is a placeholder test for future evaluation of chunk sizes.
        // Currently, TargetChunkCharacters is set to 1200, which prioritizes precise retrieval.
        // In the future, we will compare 800 vs 1200 vs 2000 size quality on real PDFs.

        var options = new ChunkingOptions();

        Assert.Equal(300, options.Default.TargetTokens);
        Assert.Equal(450, options.Default.MaxTokens);
        Assert.Equal(40, options.Default.OverlapTokens);
    }
}
