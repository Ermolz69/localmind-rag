namespace KnowledgeApp.Contracts.Rag;

/// <summary>Generated RAG answer with source citations.</summary>
/// <param name="Answer">Assistant answer text.</param>
/// <param name="Sources">Document chunks used as answer context.</param>
public sealed record RagAnswerDto(string Answer, IReadOnlyList<RagSourceDto> Sources);

