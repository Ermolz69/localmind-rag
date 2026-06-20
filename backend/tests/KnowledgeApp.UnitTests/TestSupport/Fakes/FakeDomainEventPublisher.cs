using KnowledgeApp.Application.Abstractions;

namespace KnowledgeApp.UnitTests.TestSupport.Fakes;

internal sealed class FakeDomainEventPublisher : IDomainEventPublisher
{
    public List<IDomainEvent> PublishedEvents { get; } = [];

    public Task PublishAsync<TEvent>(TEvent domainEvent, CancellationToken cancellationToken = default)
        where TEvent : IDomainEvent
    {
        PublishedEvents.Add(domainEvent);
        return Task.CompletedTask;
    }
}
