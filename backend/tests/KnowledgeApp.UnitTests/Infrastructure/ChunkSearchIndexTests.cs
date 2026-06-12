using KnowledgeApp.Domain.Entities;
using KnowledgeApp.Domain.Enums;
using KnowledgeApp.Infrastructure.Services.Search;

namespace KnowledgeApp.UnitTests.Infrastructure;

public sealed class ChunkSearchIndexTests
{
    [Fact]
    public async Task SearchDocumentCandidatesAsync_Should_Filter_By_Bucket_Date_And_File_Type()
    {
        await using ApplicationTestDatabase database = await ApplicationTestDatabase.CreateAsync();
        Guid selectedBucketId = Guid.NewGuid();
        DateTimeOffset selectedDate = new(2026, 06, 09, 12, 0, 0, TimeSpan.Zero);
        (Guid selectedDocumentId, Guid selectedChunkId) = await AddDocumentChunkAsync(
            database,
            "Selected.pdf",
            "shared marker selected",
            selectedBucketId,
            selectedDate,
            FileType.Pdf);
        await AddDocumentChunkAsync(
            database,
            "Other bucket.pdf",
            "shared marker other bucket",
            Guid.NewGuid(),
            selectedDate,
            FileType.Pdf);
        await AddDocumentChunkAsync(
            database,
            "Other date.pdf",
            "shared marker other date",
            selectedBucketId,
            selectedDate.AddDays(-5),
            FileType.Pdf);
        await AddDocumentChunkAsync(
            database,
            "Other type.txt",
            "shared marker other type",
            selectedBucketId,
            selectedDate,
            FileType.PlainText);
        ChunkSearchIndex index = new(database.Context);

        IReadOnlyList<Application.Abstractions.DocumentChunkCandidate> results =
            await index.SearchDocumentCandidatesAsync(
                ["shared", "marker"],
                selectedBucketId,
                documentId: null,
                tags: null,
                maxCount: 10,
                dateFrom: selectedDate.Date,
                dateTo: selectedDate.Date,
                fileType: "pdf");

        Application.Abstractions.DocumentChunkCandidate result = Assert.Single(results);
        Assert.Equal(selectedDocumentId, result.DocumentId);
        Assert.Equal(selectedChunkId, result.ChunkId);
    }

    private static async Task<(Guid DocumentId, Guid ChunkId)> AddDocumentChunkAsync(
        ApplicationTestDatabase database,
        string documentName,
        string chunkText,
        Guid bucketId,
        DateTimeOffset createdAt,
        FileType fileType)
    {
        Document document = new()
        {
            BucketId = bucketId,
            CreatedAt = createdAt,
            Name = documentName,
            Status = DocumentStatus.Indexed,
        };
        DocumentChunk chunk = new()
        {
            DocumentId = document.Id,
            Index = 0,
            Text = chunkText,
        };
        DocumentFile file = new()
        {
            DocumentId = document.Id,
            FileName = documentName,
            LocalPath = $"runtime/app/files/{document.Id:N}",
            ContentHash = document.Id.ToString("N"),
            FileType = fileType,
            SizeBytes = chunkText.Length,
        };

        database.Context.Documents.Add(document);
        database.Context.DocumentChunks.Add(chunk);
        database.Context.DocumentFiles.Add(file);
        await database.Context.SaveChangesAsync();

        return (document.Id, chunk.Id);
    }
}
