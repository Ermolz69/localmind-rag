using KnowledgeApp.Domain.Entities;

namespace KnowledgeApp.Infrastructure.Services.Ingestion.Chunking;

public interface ITextStructureParser
{
    bool CanParse(string text);

    IReadOnlyList<DocumentBlock> Parse(string text);
}
