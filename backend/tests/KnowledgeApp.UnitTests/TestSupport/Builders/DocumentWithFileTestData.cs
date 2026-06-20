using KnowledgeApp.Domain.Entities;
using KnowledgeApp.Domain.Enums;

namespace KnowledgeApp.UnitTests.TestSupport.Builders;

internal sealed class DocumentWithFileTestData
{
    public Guid DocumentId { get; }
    public Guid FileId { get; }
    public string LocalPath { get; }
    public FileType FileType { get; }

    private DocumentWithFileTestData(Guid documentId, Guid fileId, string localPath, FileType fileType)
    {
        DocumentId = documentId;
        FileId = fileId;
        LocalPath = localPath;
        FileType = fileType;
    }

    public static Task<DocumentWithFileTestData> CreateAsync(
        ApplicationTestDatabase database,
        ManagedFileTestStorage storage,
        string fileName,
        FileType fileType,
        string content)
        => CreateCoreAsync(
            database,
            storage,
            fileName,
            fileType,
            async path => await File.WriteAllTextAsync(path, content));

    public static Task<DocumentWithFileTestData> CreateAsync(
        ApplicationTestDatabase database,
        ManagedFileTestStorage storage,
        string fileName,
        FileType fileType,
        byte[] content)
        => CreateCoreAsync(
            database,
            storage,
            fileName,
            fileType,
            async path => await File.WriteAllBytesAsync(path, content));

    private static async Task<DocumentWithFileTestData> CreateCoreAsync(
        ApplicationTestDatabase database,
        ManagedFileTestStorage storage,
        string fileName,
        FileType fileType,
        Func<string, Task> writeContent)
    {
        Document document = new() { Name = fileName, Status = DocumentStatus.Uploaded };
        database.Context.Documents.Add(document);

        string dir = Path.Combine(storage.FilesDirectory, document.Id.ToString("N"));
        Directory.CreateDirectory(dir);
        string localPath = Path.Combine(dir, fileName);

        await writeContent(localPath);

        DocumentFile documentFile = new()
        {
            DocumentId = document.Id,
            FileName = fileName,
            FileType = fileType,
            LocalPath = localPath,
            SizeBytes = new FileInfo(localPath).Length,
        };

        database.Context.DocumentFiles.Add(documentFile);
        await database.Context.SaveChangesAsync();

        return new DocumentWithFileTestData(document.Id, documentFile.Id, localPath, fileType);
    }
}
