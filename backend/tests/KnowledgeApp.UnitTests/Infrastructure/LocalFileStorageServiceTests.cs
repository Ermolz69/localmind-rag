using System.Security.Cryptography;
using KnowledgeApp.Application.Abstractions;
using KnowledgeApp.Infrastructure.Services;

namespace KnowledgeApp.UnitTests.Infrastructure;

public sealed class LocalFileStorageServiceTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), $"localmind-storage-tests-{Guid.NewGuid():N}");

    [Fact]
    public async Task SaveAsync_Should_Save_File_Under_Document_Directory_And_Hash_Saved_Content()
    {
        var paths = new TestAppPathProvider(root);
        var storage = new LocalFileStorageService(paths);
        var documentId = Guid.NewGuid();
        var bytes = "saved content"u8.ToArray();
        await using var content = new MemoryStream(bytes);

        var stored = await storage.SaveAsync(content, documentId, "../notes.txt");

        var expectedPath = Path.Combine(paths.FilesDirectory, documentId.ToString(), "notes.txt");
        Assert.Equal("notes.txt", stored.FileName);
        Assert.Equal(expectedPath, stored.LocalPath);
        Assert.True(File.Exists(expectedPath));
        Assert.Equal(bytes.Length, stored.SizeBytes);
        Assert.Equal(Convert.ToHexString(SHA256.HashData(bytes)), stored.ContentHash);
    }

    public void Dispose()
    {
        if (Directory.Exists(root))
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private sealed class TestAppPathProvider(string root) : IAppPathProvider
    {
        public string DataDirectory => Path.Combine(root, "data");
        public string DatabasePath => Path.Combine(DataDirectory, "knowledge-app.db");
        public string FilesDirectory => Path.Combine(root, "files");
        public string IndexDirectory => Path.Combine(root, "indexes");
        public string LogsDirectory => Path.Combine(root, "logs");
    }
}
