namespace KnowledgeApp.Contracts.Rag;

public sealed record RagAnswerDto(string Answer, IReadOnlyList<RagSourceDto> Sources);


