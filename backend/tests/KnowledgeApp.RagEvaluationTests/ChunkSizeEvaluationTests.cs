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
        // This is a placeholder test for future evaluation of token-based chunk sizes.
        // Current defaults prioritize precise retrieval while keeping a hard maximum chunk size.
        // Future evaluation should compare smaller and larger token profiles on real PDFs.

        var options = new ChunkingOptions();

        Assert.Equal(300, options.Default.TargetTokens);
        Assert.Equal(450, options.Default.MaxTokens);
        Assert.Equal(40, options.Default.OverlapTokens);
        Assert.Equal(3, options.ChunkingVersion);
        Assert.Equal("structure-aware-token-v3", options.ChunkingAlgorithmId);
    }
}
