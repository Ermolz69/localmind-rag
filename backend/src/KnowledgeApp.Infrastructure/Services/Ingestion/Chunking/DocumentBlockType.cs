namespace KnowledgeApp.Infrastructure.Services.Ingestion.Chunking;

public enum DocumentBlockType
{
    Paragraph,
    Heading,
    List,
    Code,
    Table,
    Quote
}
