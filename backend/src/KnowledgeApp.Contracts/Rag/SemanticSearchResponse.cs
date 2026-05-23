namespace KnowledgeApp.Contracts.Rag;

/// <summary>Semantic search result set.</summary>
/// <param name="Sources">Source chunks ranked by semantic similarity.</param>
public sealed record SemanticSearchResponse(IReadOnlyList<RagSourceDto> Sources);
