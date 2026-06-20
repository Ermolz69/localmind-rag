namespace KnowledgeApp.Infrastructure.Options;

public sealed class DocumentPreviewOptions
{
    public const string SectionName = "DocumentPreview";

    public string? LibreOfficePath { get; set; }

    public int ConversionTimeoutSeconds { get; set; } = 60;
}
