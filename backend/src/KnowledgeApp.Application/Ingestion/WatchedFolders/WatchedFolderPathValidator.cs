using KnowledgeApp.Contracts.Settings;

namespace KnowledgeApp.Application.Ingestion.WatchedFolders;

public sealed class WatchedFolderPathValidator : IWatchedFolderPathValidator
{
    public IReadOnlyList<string> Validate(
        string path,
        RuntimePathsSettingsDto runtimePaths,
        IReadOnlyList<string> configuredFolderPaths)
    {
        List<string> errors = [];

        if (string.IsNullOrWhiteSpace(path))
        {
            errors.Add("Watched folder path is required.");
            return errors;
        }

        string trimmedPath = path.Trim();

        if (!Path.IsPathFullyQualified(trimmedPath))
        {
            errors.Add("Watched folder path must be absolute.");
            return errors;
        }

        string fullPath;

        try
        {
            fullPath = NormalizeDirectoryPath(trimmedPath);
        }
        catch (Exception)
        {
            errors.Add("Watched folder path is invalid.");
            return errors;
        }

        if (!Directory.Exists(fullPath))
        {
            errors.Add("Watched folder path must exist.");
        }

        if (IsRootDirectory(fullPath))
        {
            errors.Add("Watched folder path cannot be a drive root.");
        }

        if (IsSystemDirectory(fullPath))
        {
            errors.Add("Watched folder path cannot be a system directory.");
        }

        if (OverlapsRuntimePath(fullPath, runtimePaths))
        {
            errors.Add("Watched folder path cannot overlap application runtime storage paths.");
        }

        if (OverlapsAnotherWatchedFolder(fullPath, configuredFolderPaths))
        {
            errors.Add("Watched folder path cannot overlap another watched folder.");
        }

        return errors;
    }

    private static string NormalizeDirectoryPath(string path)
    {
        return TrimDirectorySeparator(Path.GetFullPath(path));
    }

    private static bool IsRootDirectory(string path)
    {
        string? root = Path.GetPathRoot(path);

        return !string.IsNullOrWhiteSpace(root) &&
            string.Equals(
                TrimDirectorySeparator(root),
                TrimDirectorySeparator(path),
                PathComparison);
    }

    private static bool IsSystemDirectory(string path)
    {
        string windowsPath = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
        string programFilesPath = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        string programFilesX86Path = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);

        return IsSameOrChildPath(path, windowsPath) ||
            IsSameOrChildPath(path, programFilesPath) ||
            IsSameOrChildPath(path, programFilesX86Path);
    }

    private static bool OverlapsRuntimePath(string watchedFolderPath, RuntimePathsSettingsDto runtimePaths)
    {
        string[] runtimePathsToCheck =
        [
            runtimePaths.DataPath,
            runtimePaths.DatabasePath,
            runtimePaths.FilesPath,
            runtimePaths.IndexPath,
            runtimePaths.LogsPath
        ];

        foreach (string runtimePath in runtimePathsToCheck)
        {
            if (string.IsNullOrWhiteSpace(runtimePath))
            {
                continue;
            }

            string normalizedRuntimePath;

            try
            {
                normalizedRuntimePath = NormalizeDirectoryPath(runtimePath);
            }
            catch (Exception)
            {
                continue;
            }

            if (IsSameOrChildPath(watchedFolderPath, normalizedRuntimePath) ||
                IsSameOrChildPath(normalizedRuntimePath, watchedFolderPath))
            {
                return true;
            }
        }

        return false;
    }

    private static bool OverlapsAnotherWatchedFolder(
        string watchedFolderPath,
        IReadOnlyList<string> configuredFolderPaths)
    {
        int samePathCount = 0;

        foreach (string configuredFolderPath in configuredFolderPaths)
        {
            if (string.IsNullOrWhiteSpace(configuredFolderPath))
            {
                continue;
            }

            string trimmedConfiguredPath = configuredFolderPath.Trim();

            if (!Path.IsPathFullyQualified(trimmedConfiguredPath))
            {
                continue;
            }

            string normalizedConfiguredPath;

            try
            {
                normalizedConfiguredPath = NormalizeDirectoryPath(trimmedConfiguredPath);
            }
            catch (Exception)
            {
                continue;
            }

            if (string.Equals(watchedFolderPath, normalizedConfiguredPath, PathComparison))
            {
                samePathCount++;

                if (samePathCount > 1)
                {
                    return true;
                }

                continue;
            }

            if (IsSameOrChildPath(watchedFolderPath, normalizedConfiguredPath) ||
                IsSameOrChildPath(normalizedConfiguredPath, watchedFolderPath))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsSameOrChildPath(string candidatePath, string parentPath)
    {
        if (string.IsNullOrWhiteSpace(candidatePath) || string.IsNullOrWhiteSpace(parentPath))
        {
            return false;
        }

        string normalizedCandidate = TrimDirectorySeparator(candidatePath);
        string normalizedParent = TrimDirectorySeparator(parentPath);

        return string.Equals(normalizedCandidate, normalizedParent, PathComparison) ||
            normalizedCandidate.StartsWith(
                normalizedParent + Path.DirectorySeparatorChar,
                PathComparison) ||
            normalizedCandidate.StartsWith(
                normalizedParent + Path.AltDirectorySeparatorChar,
                PathComparison);
    }

    private static string TrimDirectorySeparator(string path)
    {
        return path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
    }

    private static StringComparison PathComparison =>
        OperatingSystem.IsWindows()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;
}
