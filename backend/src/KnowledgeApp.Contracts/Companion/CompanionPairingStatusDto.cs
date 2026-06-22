namespace KnowledgeApp.Contracts.Companion;

/// <summary>Current state of the Companion Mode pairing session.</summary>
/// <param name="Active">True when a pairing session is active and not expired.</param>
/// <param name="ExpiresAt">When the active session expires, if any.</param>
/// <param name="ExpiresInSeconds">Seconds until the active session expires, or 0 when inactive.</param>
public sealed record CompanionPairingStatusDto(
    bool Active,
    DateTimeOffset? ExpiresAt,
    int ExpiresInSeconds);
