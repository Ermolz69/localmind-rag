using System.Text.Json.Serialization;

namespace KnowledgeApp.Contracts.Documents;

/// <summary>Supported document preview categories.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<DocumentPreviewKind>))]
public enum DocumentPreviewKind
{
    /// <summary>PDF preview served as a read-only file stream.</summary>
    Pdf,

    /// <summary>Plain text preview returned inline in JSON.</summary>
    Text,

    /// <summary>Markdown preview returned inline in JSON.</summary>
    Markdown,

    /// <summary>HTML preview returned inline in JSON.</summary>
    Html,

    /// <summary>Image preview served as a read-only file stream.</summary>
    Image,

    /// <summary>The document format is not previewable by the current API.</summary>
    Unsupported,

    /// <summary>The preview could not be prepared because of a controlled failure.</summary>
    Error,
}
