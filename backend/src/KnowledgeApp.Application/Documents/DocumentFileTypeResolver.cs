using KnowledgeApp.Domain.Enums;

namespace KnowledgeApp.Application.Documents;

public static class DocumentFileTypeResolver
{
    private static readonly IReadOnlyDictionary<string, FileType> ExtensionMap = new Dictionary<string, FileType>(StringComparer.OrdinalIgnoreCase)
    {
        [".pdf"] = FileType.Pdf,
        [".docx"] = FileType.Docx,
        [".pptx"] = FileType.Pptx,
        [".md"] = FileType.Markdown,
        [".markdown"] = FileType.Markdown,
        [".txt"] = FileType.PlainText,
        [".html"] = FileType.Html,
        [".htm"] = FileType.Html,
    };

    public static bool IsSupported(string fileName) => ExtensionMap.ContainsKey(Path.GetExtension(fileName));

    public static FileType Resolve(string fileName) =>
        ExtensionMap.TryGetValue(Path.GetExtension(fileName), out var fileType) ? fileType : FileType.Unknown;
}
