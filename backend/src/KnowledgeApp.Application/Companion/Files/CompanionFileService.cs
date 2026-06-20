using KnowledgeApp.Application.Common.Errors;
using KnowledgeApp.Application.Common.Results;
using KnowledgeApp.Application.Documents;
using KnowledgeApp.Application.Settings;
using KnowledgeApp.Contracts.Companion;
using KnowledgeApp.Contracts.Documents;
using KnowledgeApp.Contracts.Settings;

namespace KnowledgeApp.Application.Companion.Files;

/// <inheritdoc />
public sealed class CompanionFileService(
    ISettingsService settingsService,
    UploadDocumentHandler uploadHandler) : ICompanionFileService
{
    private static readonly EnumerationOptions EnumerationOptions = new()
    {
        IgnoreInaccessible = true,
        RecurseSubdirectories = false,
    };

    public async Task<CompanionRootsResponse> GetRootsAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<string> roots = await GetAllowedRootsAsync(cancellationToken);

        CompanionFileRootDto[] dtos = roots
            .Select(root => new CompanionFileRootDto(LeafName(root), root))
            .ToArray();

        return new CompanionRootsResponse(dtos);
    }

    public async Task<Result<CompanionBrowseResponse>> BrowseAsync(
        string path,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<string> roots = await GetAllowedRootsAsync(cancellationToken);

        if (!TryNormalize(path, out string fullPath) || !IsWithinAllowedRoots(fullPath, roots))
        {
            return Result<CompanionBrowseResponse>.Failure(NotAllowed());
        }

        if (!Directory.Exists(fullPath))
        {
            return Result<CompanionBrowseResponse>.Failure(NotFound());
        }

        List<CompanionFileEntryDto> entries = [];

        try
        {
            foreach (string directory in Directory.EnumerateDirectories(fullPath, "*", EnumerationOptions))
            {
                entries.Add(new CompanionFileEntryDto(LeafName(directory), directory, IsDirectory: true));
            }

            foreach (string file in Directory.EnumerateFiles(fullPath, "*", EnumerationOptions))
            {
                if (DocumentFileTypeResolver.IsSupported(file))
                {
                    entries.Add(new CompanionFileEntryDto(Path.GetFileName(file), file, IsDirectory: false));
                }
            }
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return Result<CompanionBrowseResponse>.Failure(NotFound());
        }

        List<CompanionFileEntryDto> ordered = entries
            .OrderByDescending(entry => entry.IsDirectory)
            .ThenBy(entry => entry.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        string? parent = Path.GetDirectoryName(fullPath);
        string? parentPath = parent is not null && IsWithinAllowedRoots(parent, roots) ? parent : null;

        return Result<CompanionBrowseResponse>.Success(
            new CompanionBrowseResponse(fullPath, parentPath, ordered));
    }

    public async Task<Result<UploadDocumentResponse>> AddFileAsync(
        string path,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<string> roots = await GetAllowedRootsAsync(cancellationToken);

        if (!TryNormalize(path, out string fullPath) || !IsWithinAllowedRoots(fullPath, roots))
        {
            return Result<UploadDocumentResponse>.Failure(NotAllowed());
        }

        if (!File.Exists(fullPath))
        {
            return Result<UploadDocumentResponse>.Failure(NotFound());
        }

        await using FileStream stream = new(
            fullPath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.ReadWrite | FileShare.Delete);

        UploadDocumentCommand command = new(
            Content: stream,
            FileName: Path.GetFileName(fullPath),
            ContentType: null,
            Length: stream.Length,
            BucketId: null);

        return await uploadHandler.HandleAsync(command, cancellationToken);
    }

    private async Task<IReadOnlyList<string>> GetAllowedRootsAsync(CancellationToken cancellationToken)
    {
        AppSettingsDto settings = await settingsService.GetAsync(cancellationToken);
        IReadOnlyList<string>? configured = settings.CompanionMode?.AllowedFolders;

        if (configured is null || configured.Count == 0)
        {
            return [];
        }

        List<string> normalized = [];

        foreach (string folder in configured)
        {
            if (TryNormalize(folder, out string fullPath))
            {
                normalized.Add(fullPath);
            }
        }

        return normalized;
    }

    private static bool IsWithinAllowedRoots(string fullPath, IReadOnlyList<string> roots)
    {
        foreach (string root in roots)
        {
            if (fullPath.Equals(root, PathComparison)
                || fullPath.StartsWith(root + Path.DirectorySeparatorChar, PathComparison))
            {
                return true;
            }
        }

        return false;
    }

    private static bool TryNormalize(string path, out string fullPath)
    {
        fullPath = string.Empty;

        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        try
        {
            fullPath = Path.GetFullPath(path.Trim())
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    private static string LeafName(string fullPath)
    {
        string name = Path.GetFileName(fullPath);
        return string.IsNullOrEmpty(name) ? fullPath : name;
    }

    // Not-allowed and not-found both map to NotFound with generic messages so the
    // phone cannot probe the disk for paths outside the allowed roots.
    private static ApplicationError NotAllowed()
    {
        return ApplicationErrors.NotFound(
            ErrorCodes.Companion.PathNotAllowed,
            "That location is not available.");
    }

    private static ApplicationError NotFound()
    {
        return ApplicationErrors.NotFound(
            ErrorCodes.Companion.PathNotFound,
            "That location is not available.");
    }

    private static StringComparison PathComparison =>
        OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
}
