using KnowledgeApp.Application.Abstractions;
using KnowledgeApp.Contracts.Companion;

namespace KnowledgeApp.Infrastructure.Services;

/// <inheritdoc />
public sealed class CompanionActivityFeed(IDateTimeProvider dateTimeProvider) : ICompanionActivityFeed
{
    private const int Capacity = 100;

    private readonly object gate = new();
    private readonly LinkedList<CompanionActivityEventDto> events = new();

    public void Publish(string kind, string message, string? detail = null)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        CompanionActivityEventDto item = new(
            Id: Guid.NewGuid(),
            Timestamp: dateTimeProvider.UtcNow,
            Kind: kind,
            Message: message,
            Detail: detail);

        lock (gate)
        {
            events.AddFirst(item);

            while (events.Count > Capacity)
            {
                events.RemoveLast();
            }
        }
    }

    public IReadOnlyList<CompanionActivityEventDto> GetRecent(int limit)
    {
        int take = Math.Clamp(limit, 1, Capacity);

        lock (gate)
        {
            return events.Take(take).ToArray();
        }
    }
}
