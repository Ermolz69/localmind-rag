using KnowledgeApp.Application.Abstractions;
using KnowledgeApp.Infrastructure.Options;
using Microsoft.Extensions.Options;

namespace KnowledgeApp.Infrastructure.Runtime;

public sealed class AppPathProvider(IOptions<LocalRuntimeOptions> options) : IAppPathProvider
{
    private readonly LocalRuntimeOptions options = options.Value;
    private readonly string rootPath = ResolveRootPath();

    public string AppRootDirectory => rootPath;
    public string DataDirectory => FullPath(options.DataPath);
    public string DatabasePath => FullPath(options.DatabasePath);
    public string FilesDirectory => FullPath(options.FilesPath);
    public string IndexDirectory => FullPath(options.IndexPath);
    public string LogsDirectory => FullPath(options.LogsPath);

    private string FullPath(string path) => Path.GetFullPath(path, rootPath);

    private static string ResolveRootPath()
    {
        var configured = Environment.GetEnvironmentVariable("KNOWLEDGE_APP_ROOT");
        if (!string.IsNullOrWhiteSpace(configured))
        {
            return Path.GetFullPath(configured);
        }

        var current = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "pnpm-workspace.yaml")) ||
                Directory.Exists(Path.Combine(current.FullName, "runtime")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        return Directory.GetCurrentDirectory();
    }
}
