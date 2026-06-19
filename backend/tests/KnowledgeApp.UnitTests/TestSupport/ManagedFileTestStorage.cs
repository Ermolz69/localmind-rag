using System.Text;

namespace KnowledgeApp.UnitTests.TestSupport;

internal sealed class ManagedFileTestStorage : IAsyncDisposable
{
    private readonly string rootDirectory;

    public string FilesDirectory { get; }
    public string PreviewDirectory { get; }

    private ManagedFileTestStorage(string rootDirectory)
    {
        this.rootDirectory = rootDirectory;
        FilesDirectory = Path.Combine(rootDirectory, "runtime", "app", "files");
        PreviewDirectory = Path.Combine(rootDirectory, "runtime", "app", "preview");
        Directory.CreateDirectory(FilesDirectory);
        Directory.CreateDirectory(PreviewDirectory);
    }

    public static ManagedFileTestStorage Create()
    {
        string root = Path.Combine(
            Path.GetTempPath(),
            $"localmind-managed-{Guid.NewGuid():N}");
        return new ManagedFileTestStorage(root);
    }

    public async Task<string> WriteFileAsync(Guid documentId, string fileName, string content)
    {
        string dir = Path.Combine(FilesDirectory, documentId.ToString("N"));
        Directory.CreateDirectory(dir);
        string path = Path.Combine(dir, fileName);
        await File.WriteAllTextAsync(path, content, new UTF8Encoding(false, false));
        return path;
    }

    public async Task<string> WriteFileAsync(Guid documentId, string fileName, byte[] content)
    {
        string dir = Path.Combine(FilesDirectory, documentId.ToString("N"));
        Directory.CreateDirectory(dir);
        string path = Path.Combine(dir, fileName);
        await File.WriteAllBytesAsync(path, content);
        return path;
    }

    public ValueTask DisposeAsync()
    {
        if (Directory.Exists(rootDirectory))
            Directory.Delete(rootDirectory, recursive: true);
        return ValueTask.CompletedTask;
    }
}
