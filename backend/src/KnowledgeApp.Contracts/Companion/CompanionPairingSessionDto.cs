namespace KnowledgeApp.Contracts.Companion;

/// <summary>
/// An active, time-limited Companion Mode pairing session. The desktop renders
/// <see cref="PairingUrl"/> as a QR code for a phone to scan.
/// </summary>
/// <param name="Token">Opaque single-use pairing token.</param>
/// <param name="PairingUrl">URL encoded into the QR code that the phone opens.</param>
/// <param name="ExpiresAt">When the pairing session expires.</param>
/// <param name="ExpiresInSeconds">Seconds until the session expires.</param>
public sealed record CompanionPairingSessionDto(
    string Token,
    string PairingUrl,
    DateTimeOffset ExpiresAt,
    int ExpiresInSeconds);
