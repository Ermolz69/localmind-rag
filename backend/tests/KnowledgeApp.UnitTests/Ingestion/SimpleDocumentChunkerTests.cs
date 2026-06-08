using KnowledgeApp.Infrastructure.Services;
using KnowledgeApp.Infrastructure.Options;
using Microsoft.Extensions.Options;

namespace KnowledgeApp.UnitTests.Ingestion;

public sealed class SimpleDocumentChunkerTests
{
    [Fact]
    public void Split_Should_Return_Paragraph_Aware_Chunks_In_Stable_Order()
    {
        SimpleDocumentChunker? chunker = new SimpleDocumentChunker();
        string? text = "First paragraph.\n\nSecond paragraph.\n\nThird paragraph.";

        IReadOnlyList<string>? chunks = chunker.Split(text);

        Assert.Single(chunks);
        Assert.Equal("First paragraph.\n\nSecond paragraph.\n\nThird paragraph.", chunks[0]);
    }

    [Fact]
    public void Split_Should_Not_Create_Empty_Chunks()
    {
        SimpleDocumentChunker? chunker = new SimpleDocumentChunker();

        IReadOnlyList<string>? chunks = chunker.Split("  \n\n  \r\n ");

        Assert.Empty(chunks);
    }

    [Fact]
    public void Split_Should_Split_Long_Paragraph()
    {
        SimpleDocumentChunker? chunker = new SimpleDocumentChunker();
        string? text = new string('a', 2500);

        IReadOnlyList<string>? chunks = chunker.Split(text);

        Assert.Equal(3, chunks.Count);
        Assert.All(chunks, chunk => Assert.InRange(chunk.Length, 1, 1200));
        Assert.StartsWith(new string('a', 150), chunks[1], StringComparison.Ordinal);
    }

    [Fact]
    public void SplitDetailed_Should_Preserve_Heading_Path()
    {
        SimpleDocumentChunker chunker = new();
        string text = "# Product\n\nIntro paragraph.\n\n## Search\n\nSearch details.";

        IReadOnlyList<KnowledgeApp.Application.Abstractions.DocumentChunkText> chunks =
            chunker.SplitDetailed(text);

        Assert.Contains(chunks, chunk => chunk.HeadingPath == "Product > Search");
    }

    [Fact]
    public void Split_Should_Merge_Small_Paragraphs_To_Target_Size()
    {
        SimpleDocumentChunker chunker = new(
            Options.Create(new ChunkingOptions
            {
                TargetChunkCharacters = 45,
                MaxChunkCharacters = 120,
                MinChunkCharacters = 20,
            }));

        IReadOnlyList<string> chunks = chunker.Split("One short paragraph.\n\nTwo short paragraph.\n\nThree short paragraph.");

        Assert.Equal(2, chunks.Count);
        Assert.Contains("One short paragraph.", chunks[0], StringComparison.Ordinal);
        Assert.Contains("Two short paragraph.", chunks[0], StringComparison.Ordinal);
    }
}
