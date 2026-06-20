namespace KnowledgeApp.Contracts.Companion;

/// <summary>
/// Completes a pairing session and registers the calling device as trusted. In a
/// later stage this is what a phone calls over the local-network transport; today
/// it is reachable only on the loopback API.
/// </summary>
/// <param name="Token">The pairing token from the scanned QR code.</param>
/// <param name="DeviceName">Human-friendly device name, e.g. "Redmi Note".</param>
/// <param name="Platform">Client platform, e.g. "Chrome".</param>
public sealed record ConfirmCompanionPairingRequest(
    string Token,
    string DeviceName,
    string Platform);
