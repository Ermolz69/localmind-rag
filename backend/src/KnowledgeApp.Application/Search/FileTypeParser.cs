using KnowledgeApp.Domain.Enums;

namespace KnowledgeApp.Application.Search;

public static class FileTypeParser
{
    private static readonly HashSet<string> ValidTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "pdf", "docx", "pptx", "markdown", "md", "txt", "text", "plaintext", "html"
    };

    public static FileType Parse(string value) => value.ToLowerInvariant() switch
    {
        "pdf" => FileType.Pdf,
        "docx" => FileType.Docx,
        "pptx" => FileType.Pptx,
        "markdown" or "md" => FileType.Markdown,
        "txt" or "text" or "plaintext" => FileType.PlainText,
        "html" => FileType.Html,
        _ => FileType.Unknown,
    };

    public static bool IsValid(string value) => ValidTypes.Contains(value);
}
