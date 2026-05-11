namespace KnowledgeApp.Contracts.Rag;

public sealed record ChatMessageRequest(string Content);
public sealed record RagAnswerDto(string Answer, IReadOnlyList<RagSourceDto> Sources);
public sealed record RagSourceDto(Guid DocumentId, string DocumentName, Guid ChunkId, int? PageNumber, double Score, string Snippet);
public sealed record SemanticSearchRequest(string Query, int Limit = 8);
