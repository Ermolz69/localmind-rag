using KnowledgeApp.Application.Ingestion.IncrementalIndexing;
using KnowledgeApp.Infrastructure.Services;

namespace KnowledgeApp.UnitTests.Ingestion;

public sealed class ContentHashServiceTests
{
    [Fact]
    public void ComputeChunkHash_Should_ReturnSameHash_ForSameText()
    {
        Sha256ContentHashService service = new Sha256ContentHashService();

        string firstHash = service.ComputeChunkHash("Same chunk text");
        string secondHash = service.ComputeChunkHash("Same chunk text");

        Assert.Equal(firstHash, secondHash);
    }

    [Fact]
    public void ComputeChunkHash_Should_ReturnDifferentHash_ForDifferentText()
    {
        Sha256ContentHashService service = new Sha256ContentHashService();

        string firstHash = service.ComputeChunkHash("Original chunk text");
        string secondHash = service.ComputeChunkHash("Modified chunk text");

        Assert.NotEqual(firstHash, secondHash);
    }

    [Fact]
    public void ComputeChunkHash_Should_NormalizeLineEndings()
    {
        Sha256ContentHashService service = new Sha256ContentHashService();

        string windowsHash = service.ComputeChunkHash("First line\r\nSecond line");
        string unixHash = service.ComputeChunkHash("First line\nSecond line");

        Assert.Equal(windowsHash, unixHash);
    }

    [Fact]
    public void ComputeChunkHash_Should_ReturnSha256HexHash()
    {
        Sha256ContentHashService service = new Sha256ContentHashService();

        string hash = service.ComputeChunkHash("Chunk text");

        Assert.Equal(64, hash.Length);
        Assert.Matches("^[a-f0-9]{64}$", hash);
    }

    [Fact]
    public void ComputeDocumentHash_Should_ReturnSameHash_ForSameChunkHashesInSameOrder()
    {
        Sha256ContentHashService service = new Sha256ContentHashService();

        string firstHash = service.ComputeDocumentHash(
            ["chunk-hash-1", "chunk-hash-2", "chunk-hash-3"],
            IndexingVersions.CurrentDocumentIndexVersion);

        string secondHash = service.ComputeDocumentHash(
            ["chunk-hash-1", "chunk-hash-2", "chunk-hash-3"],
            IndexingVersions.CurrentDocumentIndexVersion);

        Assert.Equal(firstHash, secondHash);
    }

    [Fact]
    public void ComputeDocumentHash_Should_ReturnDifferentHash_WhenChunkOrderChanges()
    {
        Sha256ContentHashService service = new Sha256ContentHashService();

        string firstHash = service.ComputeDocumentHash(
            ["chunk-hash-1", "chunk-hash-2", "chunk-hash-3"],
            IndexingVersions.CurrentDocumentIndexVersion);

        string secondHash = service.ComputeDocumentHash(
            ["chunk-hash-1", "chunk-hash-3", "chunk-hash-2"],
            IndexingVersions.CurrentDocumentIndexVersion);

        Assert.NotEqual(firstHash, secondHash);
    }

    [Fact]
    public void ComputeDocumentHash_Should_ReturnDifferentHash_WhenIndexVersionChanges()
    {
        Sha256ContentHashService service = new Sha256ContentHashService();

        string firstHash = service.ComputeDocumentHash(
            ["chunk-hash-1", "chunk-hash-2"],
            1);

        string secondHash = service.ComputeDocumentHash(
            ["chunk-hash-1", "chunk-hash-2"],
            2);

        Assert.NotEqual(firstHash, secondHash);
    }
}
