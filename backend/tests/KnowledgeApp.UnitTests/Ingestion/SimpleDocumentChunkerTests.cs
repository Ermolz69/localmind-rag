using KnowledgeApp.Infrastructure.Services;

namespace KnowledgeApp.UnitTests.Ingestion;

public sealed class SimpleDocumentChunkerTests
{
    [Fact]
    public void Split_Should_Return_Paragraph_Aware_Chunks_In_Stable_Order()
    {
        var chunker = new SimpleDocumentChunker();
        var text = "First paragraph.\n\nSecond paragraph.\n\nThird paragraph.";

        var chunks = chunker.Split(text);

        Assert.Single(chunks);
        Assert.Equal("First paragraph.\n\nSecond paragraph.\n\nThird paragraph.", chunks[0]);
    }

    [Fact]
    public void Split_Should_Not_Create_Empty_Chunks()
    {
        var chunker = new SimpleDocumentChunker();

        var chunks = chunker.Split("  \n\n  \r\n ");

        Assert.Empty(chunks);
    }

    [Fact]
    public void Split_Should_Split_Long_Paragraph()
    {
        var chunker = new SimpleDocumentChunker();
        var text = new string('a', 2500);

        var chunks = chunker.Split(text);

        Assert.Equal(3, chunks.Count);
        Assert.All(chunks, chunk => Assert.InRange(chunk.Length, 1, 1200));
        Assert.Equal(2500, chunks.Sum(chunk => chunk.Length));
    }
}
