namespace KnowledgeApp.Application.Abstractions;

public interface IAppPathProvider
{
    string AppRootDirectory { get; }

    string DataDirectory { get; }

    string DatabasePath { get; }

    string FilesDirectory { get; }

    string IndexDirectory { get; }

    string LogsDirectory { get; }
}
