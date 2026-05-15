using KnowledgeApp.Application.Abstractions;

namespace KnowledgeApp.UnitTests;

internal sealed class FixedDateTimeProvider : IDateTimeProvider
{
    public DateTimeOffset UtcNow => new(2026, 5, 15, 12, 0, 0, TimeSpan.Zero);
}
