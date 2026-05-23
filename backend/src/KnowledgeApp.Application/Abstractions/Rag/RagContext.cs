using KnowledgeApp.Contracts.Rag;

namespace KnowledgeApp.Application.Abstractions;

public sealed record RagContext(IReadOnlyList<RagSourceDto> Sources, string ContextText);
