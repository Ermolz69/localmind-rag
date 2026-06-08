using System.IO.Enumeration;
using KnowledgeApp.Contracts.Settings;
using KnowledgeApp.Domain.Enums;

namespace KnowledgeApp.Application.Ingestion.WatchedFolders.Filtering;

public sealed class WatchedFileFilterService : IWatchedFileFilterService
{
    public WatchedFileFilterContext CreateContext(WatchedFoldersSettingsDto settings)
    {
        return new WatchedFileFilterContext(settings);
    }

    public WatchedFileFilterResult Evaluate(string filePath, WatchedFileFilterContext context)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return WatchedFileFilterResult.Rejected(WatchedFileFilterReason.InvalidPath);
        }

        if (ContainsIgnoredFolderSegment(filePath, context.IgnoredFolders))
        {
            return WatchedFileFilterResult.Rejected(WatchedFileFilterReason.IgnoredFolder);
        }

        string fileName = Path.GetFileName(filePath);

        if (MatchesIgnoredPattern(fileName, context.IgnoredPatterns))
        {
            return WatchedFileFilterResult.Rejected(WatchedFileFilterReason.IgnoredPattern);
        }

        FileType fileType = ResolveFileType(filePath);

        if (fileType == FileType.Unknown)
        {
            return WatchedFileFilterResult.Rejected(WatchedFileFilterReason.UnsupportedExtension);
        }

        if (context.AllowedExtensions.Count > 0)
        {
            string extension = Path.GetExtension(filePath);
            if (!context.AllowedExtensions.Contains(extension))
            {
                return WatchedFileFilterResult.Rejected(WatchedFileFilterReason.ExtensionNotAllowed);
            }
        }

        if (context.MaxFileSizeBytes.HasValue)
        {
            if (!File.Exists(filePath))
            {
                return WatchedFileFilterResult.Rejected(WatchedFileFilterReason.MissingFile);
            }

            FileInfo fileInfo = new FileInfo(filePath);

            if (fileInfo.Length > context.MaxFileSizeBytes.Value)
            {
                return WatchedFileFilterResult.Rejected(WatchedFileFilterReason.FileTooLarge);
            }
        }

        return WatchedFileFilterResult.Allowed();
    }

    private static bool ContainsIgnoredFolderSegment(
        string filePath,
        HashSet<string> ignoredFolders)
    {
        if (ignoredFolders.Count == 0)
        {
            return false;
        }

        ReadOnlySpan<char> path = filePath.AsSpan();
        int segmentStart = 0;

        for (int i = 0; i <= path.Length; i++)
        {
            bool isEnd = i == path.Length;
            bool isSeparator = !isEnd &&
                (path[i] == Path.DirectorySeparatorChar ||
                 path[i] == Path.AltDirectorySeparatorChar);

            if (!isEnd && !isSeparator)
            {
                continue;
            }

            ReadOnlySpan<char> segment = path.Slice(segmentStart, i - segmentStart);

            if (!segment.IsEmpty)
            {
                string segmentText = segment.ToString();

                if (ignoredFolders.Contains(segmentText))
                {
                    return true;
                }
            }

            segmentStart = i + 1;
        }

        return false;
    }

    private static bool MatchesIgnoredPattern(string fileName, IReadOnlyList<string> ignoredPatterns)
    {
        if (ignoredPatterns.Count == 0)
        {
            return false;
        }

        foreach (string pattern in ignoredPatterns)
        {
            if (FileSystemName.MatchesSimpleExpression(pattern, fileName, ignoreCase: true))
            {
                return true;
            }
        }

        return false;
    }

    private static FileType ResolveFileType(string filePath)
    {
        string extension = Path.GetExtension(filePath);

        return extension.ToLowerInvariant() switch
        {
            ".pdf" => FileType.Pdf,
            ".docx" => FileType.Docx,
            ".pptx" => FileType.Pptx,
            ".md" => FileType.Markdown,
            ".markdown" => FileType.Markdown,
            ".txt" => FileType.PlainText,
            ".html" => FileType.Html,
            ".htm" => FileType.Html,
            _ => FileType.Unknown
        };
    }
}
