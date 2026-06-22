namespace KnowledgeApp.Contracts.Companion;

/// <summary>A single near-real-time activity event shown in the companion feed.</summary>
/// <param name="Id">Stable event identifier.</param>
/// <param name="Timestamp">When the event happened.</param>
/// <param name="Kind">Machine-readable event kind, e.g. "ingestion.indexed".</param>
/// <param name="Message">Human-friendly description.</param>
/// <param name="Detail">Optional extra detail, e.g. a failure reason.</param>
public sealed record CompanionActivityEventDto(
    Guid Id,
    DateTimeOffset Timestamp,
    string Kind,
    string Message,
    string? Detail);

/// <summary>Recent companion activity, newest first.</summary>
/// <param name="Events">Recent activity events.</param>
public sealed record CompanionActivityResponse(IReadOnlyList<CompanionActivityEventDto> Events);
