using KnowledgeApp.Application.Abstractions;
using KnowledgeApp.Infrastructure.Options;

using Microsoft.Extensions.Options;

namespace KnowledgeApp.Infrastructure.Runtime;

public sealed class AppPathProvider(
    IOptions<StorageOptions> storageOptions,
    IOptions<DatabaseOptions> databaseOptions,
    IOptions<VectorIndexOptions> vectorIndexOptions) : IAppPathProvider
{
    private readonly StorageOptions storage = storageOptions.Value;
    private readonly DatabaseOptions database = databaseOptions.Value;
    private readonly VectorIndexOptions vectorIndex = vectorIndexOptions.Value;
    private readonly string rootPath = ResolveRootPath();

    public string AppRootDirectory => rootPath;

    public string DataDirectory => FullPath(storage.DataPath);

    public string DatabasePath => FullPath(database.DatabasePath);

    public string FilesDirectory => FullPath(storage.FilesPath);

    public string IndexDirectory => FullPath(vectorIndex.IndexPath);

    public string LogsDirectory => FullPath(storage.LogsPath);

    private string FullPath(string path) => Path.GetFullPath(path, rootPath);

    private static string ResolveRootPath()
    {
        string? configured = Environment.GetEnvironmentVariable("KNOWLEDGE_APP_ROOT");

        if (!string.IsNullOrWhiteSpace(configured))
        {
            return Path.GetFullPath(configured);
        }

        DirectoryInfo? current = new DirectoryInfo(Directory.GetCurrentDirectory());
        DirectoryInfo? runtimeRoot = null;

        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "pnpm-workspace.yaml")))
            {
                return current.FullName;
            }

            if (runtimeRoot is null &&
                Directory.Exists(Path.Combine(current.FullName, "runtime")))
            {
                runtimeRoot = current;
            }

            current = current.Parent;
        }

        return runtimeRoot?.FullName ?? Directory.GetCurrentDirectory();
    }
}
