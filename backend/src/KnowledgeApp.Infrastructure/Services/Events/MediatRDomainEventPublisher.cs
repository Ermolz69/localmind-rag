using KnowledgeApp.Application.Abstractions;
using MediatR;

namespace KnowledgeApp.Infrastructure.Services.Events;

public sealed class MediatRDomainEventPublisher(IPublisher publisher) : IDomainEventPublisher
{
    public Task PublishAsync<TEvent>(TEvent domainEvent, CancellationToken cancellationToken = default) where TEvent : IDomainEvent
    {
        return publisher.Publish(domainEvent, cancellationToken);
    }
}
