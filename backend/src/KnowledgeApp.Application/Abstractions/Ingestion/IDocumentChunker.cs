namespace KnowledgeApp.Application.Abstractions;

public interface IDocumentChunker
{
    IReadOnlyList<string> Split(string text);
}
