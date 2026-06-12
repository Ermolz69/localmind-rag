using KnowledgeApp.Application.Abstractions;
using KnowledgeApp.Domain.Entities;
using KnowledgeApp.Domain.Enums;
using KnowledgeApp.Infrastructure.Services.Search;

using Microsoft.EntityFrameworkCore;

namespace KnowledgeApp.UnitTests.Infrastructure;

public sealed class SqliteFullTextChunkSearchServiceTests
{
    [Fact]
    public async Task SearchAsync_Should_Return_Exact_Keyword_Match()
    {
        await using ApplicationTestDatabase database = await CreateDatabaseAsync();
        (Guid DocumentId, Guid ChunkId) source = await AddChunkAsync(
            database,
            "Security guide",
            "Mutating endpoints require X-LocalMind-Token.");

        SqliteFullTextChunkSearchService service = new(database.Context);
        await service.SyncDocumentAsync(source.DocumentId);

        IReadOnlyList<FullTextChunkSearchResult> results = await service.SearchAsync(
            "X-LocalMind-Token",
            new FullTextSearchOptions(Limit: 5));

        FullTextChunkSearchResult result = Assert.Single(results);
        Assert.Equal(source.DocumentId, result.DocumentId);
        Assert.Equal(source.ChunkId, result.ChunkId);
        Assert.Equal(1, result.Rank);
        Assert.Contains("X-LocalMind-Token", result.Snippet, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SearchAsync_Should_Respect_Bucket_Filter()
    {
        await using ApplicationTestDatabase database = await CreateDatabaseAsync();

        Guid selectedBucketId = Guid.NewGuid();
        (Guid SelectedDocumentId, Guid SelectedChunkId) selected = await AddChunkAsync(
            database,
            "Selected",
            "NorthGate VPN setup",
            selectedBucketId);

        (Guid OtherDocumentId, _) = await AddChunkAsync(
            database,
            "Other",
            "NorthGate VPN unrelated bucket",
            Guid.NewGuid());

        SqliteFullTextChunkSearchService service = new(database.Context);
        await service.SyncDocumentAsync(selected.SelectedDocumentId);
        await service.SyncDocumentAsync(OtherDocumentId);

        IReadOnlyList<FullTextChunkSearchResult> results = await service.SearchAsync(
            "NorthGate VPN",
            new FullTextSearchOptions(Limit: 5, BucketId: selectedBucketId));

        FullTextChunkSearchResult result = Assert.Single(results);
        Assert.Equal(selected.SelectedChunkId, result.ChunkId);
    }

    [Fact]
    public async Task SyncDocumentAsync_Should_Remove_Stale_Chunks_For_Document()
    {
        await using ApplicationTestDatabase database = await CreateDatabaseAsync();
        (Guid DocumentId, Guid OldChunkId) source = await AddChunkAsync(
            database,
            "Policy",
            "StaleOnlyTerm");

        SqliteFullTextChunkSearchService service = new(database.Context);
        await service.SyncDocumentAsync(source.DocumentId);

        DocumentChunk oldChunk = await database.Context.DocumentChunks.SingleAsync(
            chunk => chunk.Id == source.OldChunkId);

        database.Context.DocumentChunks.Remove(oldChunk);
        DocumentChunk newChunk = new()
        {
            DocumentId = source.DocumentId,
            Index = 0,
            Text = "FreshOnlyTerm"
        };
        database.Context.DocumentChunks.Add(newChunk);
        await database.Context.SaveChangesAsync();
        await service.SyncDocumentAsync(source.DocumentId);

        IReadOnlyList<FullTextChunkSearchResult> oldResults = await service.SearchAsync(
            "StaleOnlyTerm",
            new FullTextSearchOptions(Limit: 5));

        IReadOnlyList<FullTextChunkSearchResult> newResults = await service.SearchAsync(
            "FreshOnlyTerm",
            new FullTextSearchOptions(Limit: 5));

        Assert.Empty(oldResults);
        FullTextChunkSearchResult result = Assert.Single(newResults);
        Assert.Equal(newChunk.Id, result.ChunkId);
    }

    private static async Task<ApplicationTestDatabase> CreateDatabaseAsync()
    {
        ApplicationTestDatabase database = await ApplicationTestDatabase.CreateAsync();
        await database.Context.Database.ExecuteSqlRawAsync(FullTextSearchSchema.CreateSql);
        return database;
    }

    private static async Task<(Guid DocumentId, Guid ChunkId)> AddChunkAsync(
        ApplicationTestDatabase database,
        string documentName,
        string chunkText,
        Guid? bucketId = null)
    {
        Document document = new()
        {
            BucketId = bucketId,
            Name = documentName,
            Status = DocumentStatus.Indexed
        };

        DocumentChunk chunk = new()
        {
            DocumentId = document.Id,
            Index = 0,
            Text = chunkText
        };

        database.Context.Documents.Add(document);
        database.Context.DocumentChunks.Add(chunk);
        await database.Context.SaveChangesAsync();

        return (document.Id, chunk.Id);
    }
}
