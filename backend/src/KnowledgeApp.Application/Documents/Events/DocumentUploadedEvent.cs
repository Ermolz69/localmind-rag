using KnowledgeApp.Application.Abstractions;

namespace KnowledgeApp.Application.Documents;

public sealed record DocumentUploadedEvent(Guid DocumentId, DateTimeOffset Timestamp) : IDomainEvent;
