namespace KnowledgeApp.Contracts.Rag;

public sealed record SemanticSearchResponse(IReadOnlyList<RagSourceDto> Sources);
