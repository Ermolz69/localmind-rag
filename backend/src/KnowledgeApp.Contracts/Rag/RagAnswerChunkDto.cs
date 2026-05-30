namespace KnowledgeApp.Contracts.Rag;

/// <summary>Represents a progressive chunk of a streamed RAG answer.</summary>
/// <param name="Text">The generated token or text chunk.</param>
/// <param name="Sources">The sources used to generate the answer. Typically sent only on the first or last chunk.</param>
public sealed record RagAnswerChunkDto(
    string Text,
    IReadOnlyList<RagSourceDto>? Sources = null);
