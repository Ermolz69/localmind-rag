using KnowledgeApp.Application.Abstractions;

namespace KnowledgeApp.UnitTests.TestSupport.Fakes;

internal sealed class StubAppPathProvider : IAppPathProvider
{
    public StubAppPathProvider(ManagedFileTestStorage storage)
    {
        FilesDirectory = storage.FilesDirectory;
        PreviewDirectory = storage.PreviewDirectory;
        AppRootDirectory = Directory.GetParent(Directory.GetParent(FilesDirectory)!.FullName)!.FullName;
        DataDirectory = Path.Combine(AppRootDirectory, "data");
        DatabasePath = Path.Combine(DataDirectory, "db.sqlite");
        IndexDirectory = Path.Combine(AppRootDirectory, "index");
        LogsDirectory = Path.Combine(AppRootDirectory, "logs");
    }

    public string AppRootDirectory { get; }
    public string DataDirectory { get; }
    public string DatabasePath { get; }
    public string FilesDirectory { get; }
    public string PreviewDirectory { get; }
    public string IndexDirectory { get; }
    public string LogsDirectory { get; }
}
