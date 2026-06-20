using KnowledgeApp.Domain.Entities;
using KnowledgeApp.Domain.Enums;

namespace KnowledgeApp.UnitTests.TestSupport.Builders;

internal sealed class DocumentIngestionTestData : IAsyncDisposable
{
    private readonly string filePath;

    private DocumentIngestionTestData(Guid documentId, Guid jobId, string filePath)
    {
        DocumentId = documentId;
        JobId = jobId;
        this.filePath = filePath;
    }

    public Guid DocumentId { get; }
    public Guid JobId { get; }
    public string FilePath => filePath;

    public static Task<DocumentIngestionTestData> CreateAsync(
        ApplicationTestDatabase database,
        string fileName,
        FileType fileType,
        string content)
        => CreateCoreAsync(database, fileName, fileType, async path => await File.WriteAllTextAsync(path, content), content.Length);

    public static Task<DocumentIngestionTestData> CreateAsync(
        ApplicationTestDatabase database,
        string fileName,
        FileType fileType,
        byte[] content)
        => CreateCoreAsync(database, fileName, fileType, async path => await File.WriteAllBytesAsync(path, content), content.Length);

    public ValueTask DisposeAsync()
    {
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }
        return ValueTask.CompletedTask;
    }

    private static async Task<DocumentIngestionTestData> CreateCoreAsync(
        ApplicationTestDatabase database,
        string fileName,
        FileType fileType,
        Func<string, Task> writeContent,
        long sizeBytes)
    {
        Document document = new() { Name = fileName, Status = DocumentStatus.Queued };

        string tempPath = Path.Combine(
            Path.GetTempPath(),
            $"localmind-ingestion-{Guid.NewGuid():N}-{fileName}");

        await writeContent(tempPath);

        DocumentFile documentFile = new()
        {
            DocumentId = document.Id,
            FileName = fileName,
            FileType = fileType,
            LocalPath = tempPath,
            SizeBytes = sizeBytes,
        };

        IngestionJob job = new() { DocumentId = document.Id, Status = IngestionJobStatus.Pending };

        database.Context.Documents.Add(document);
        database.Context.DocumentFiles.Add(documentFile);
        database.Context.IngestionJobs.Add(job);
        await database.Context.SaveChangesAsync();

        return new DocumentIngestionTestData(document.Id, job.Id, tempPath);
    }
}
