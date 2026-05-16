namespace KnowledgeApp.Contracts.Documents;

public sealed record DocumentDto(Guid Id, string Name, string Status, DateTimeOffset CreatedAt, string? LastError = null);


