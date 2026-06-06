using KnowledgeApp.Application.Ingestion.WatchedFolders;
using KnowledgeApp.Contracts.Settings;

namespace KnowledgeApp.UnitTests.Ingestion;

public sealed class WatchedFolderPathValidatorTests : IDisposable
{
    private readonly string rootPath;
    private readonly string validWatchedPath;
    private readonly string runtimePath;

    public WatchedFolderPathValidatorTests()
    {
        rootPath = Path.Combine(Path.GetTempPath(), "localmind-watch-validator-tests", Guid.NewGuid().ToString("N"));
        validWatchedPath = Path.Combine(rootPath, "watch");
        runtimePath = Path.Combine(rootPath, "runtime");

        Directory.CreateDirectory(validWatchedPath);
        Directory.CreateDirectory(runtimePath);
        Directory.CreateDirectory(Path.Combine(runtimePath, "files"));
        Directory.CreateDirectory(Path.Combine(runtimePath, "index"));
        Directory.CreateDirectory(Path.Combine(runtimePath, "logs"));
    }

    [Fact]
    public void Validate_Should_ReturnNoErrors_ForValidExistingAbsolutePath()
    {
        WatchedFolderPathValidator validator = new WatchedFolderPathValidator();

        IReadOnlyList<string> errors = validator.Validate(
            validWatchedPath,
            CreateRuntimePaths(),
            [validWatchedPath]);

        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_Should_ReturnError_ForRelativePath()
    {
        WatchedFolderPathValidator validator = new WatchedFolderPathValidator();

        IReadOnlyList<string> errors = validator.Validate(
            "relative-watch-folder",
            CreateRuntimePaths(),
            ["relative-watch-folder"]);

        Assert.Contains(errors, error => error.Contains("absolute", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validate_Should_ReturnError_ForMissingPath()
    {
        WatchedFolderPathValidator validator = new WatchedFolderPathValidator();

        string missingPath = Path.Combine(rootPath, "missing");

        IReadOnlyList<string> errors = validator.Validate(
            missingPath,
            CreateRuntimePaths(),
            [missingPath]);

        Assert.Contains(errors, error => error.Contains("exist", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validate_Should_ReturnError_ForDriveRoot_WhenRunningOnWindows()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        WatchedFolderPathValidator validator = new WatchedFolderPathValidator();

        string root = Path.GetPathRoot(validWatchedPath)!;

        IReadOnlyList<string> errors = validator.Validate(
            root,
            CreateRuntimePaths(),
            [root]);

        Assert.Contains(errors, error => error.Contains("root", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validate_Should_ReturnError_ForSystemDirectory_WhenRunningOnWindows()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        WatchedFolderPathValidator validator = new WatchedFolderPathValidator();

        string windowsPath = Environment.GetFolderPath(Environment.SpecialFolder.Windows);

        IReadOnlyList<string> errors = validator.Validate(
            windowsPath,
            CreateRuntimePaths(),
            [windowsPath]);

        Assert.Contains(errors, error => error.Contains("system", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validate_Should_ReturnError_WhenWatchedFolderIsInsideRuntimePath()
    {
        WatchedFolderPathValidator validator = new WatchedFolderPathValidator();

        string watchedPath = Path.Combine(runtimePath, "files", "watch");
        Directory.CreateDirectory(watchedPath);

        IReadOnlyList<string> errors = validator.Validate(
            watchedPath,
            CreateRuntimePaths(),
            [watchedPath]);

        Assert.Contains(errors, error => error.Contains("runtime", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validate_Should_ReturnError_WhenRuntimePathIsInsideWatchedFolder()
    {
        WatchedFolderPathValidator validator = new WatchedFolderPathValidator();

        IReadOnlyList<string> errors = validator.Validate(
            rootPath,
            CreateRuntimePaths(),
            [rootPath]);

        Assert.Contains(errors, error => error.Contains("runtime", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validate_Should_ReturnError_ForOverlappingWatchedFolders()
    {
        WatchedFolderPathValidator validator = new WatchedFolderPathValidator();

        string childWatchedPath = Path.Combine(validWatchedPath, "child");
        Directory.CreateDirectory(childWatchedPath);

        IReadOnlyList<string> errors = validator.Validate(
            childWatchedPath,
            CreateRuntimePaths(),
            [validWatchedPath, childWatchedPath]);

        Assert.Contains(errors, error => error.Contains("overlap", StringComparison.OrdinalIgnoreCase));
    }

    public void Dispose()
    {
        if (Directory.Exists(rootPath))
        {
            try
            {
                Directory.Delete(rootPath, recursive: true);
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }
    }

    private RuntimePathsSettingsDto CreateRuntimePaths()
    {
        return new RuntimePathsSettingsDto(
            DataPath: runtimePath,
            DatabasePath: Path.Combine(runtimePath, "knowledge-app.db"),
            FilesPath: Path.Combine(runtimePath, "files"),
            IndexPath: Path.Combine(runtimePath, "index"),
            LogsPath: Path.Combine(runtimePath, "logs"));
    }
}
