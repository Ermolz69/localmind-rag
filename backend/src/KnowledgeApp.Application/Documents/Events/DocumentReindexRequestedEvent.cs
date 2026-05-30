using KnowledgeApp.Application.Abstractions;

namespace KnowledgeApp.Application.Documents;

public sealed record DocumentReindexRequestedEvent(Guid DocumentId, DateTimeOffset Timestamp) : IDomainEvent;
