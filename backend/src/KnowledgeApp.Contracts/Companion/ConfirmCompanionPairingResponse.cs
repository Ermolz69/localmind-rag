namespace KnowledgeApp.Contracts.Companion;

/// <summary>
/// Result of completing a pairing session: the registered device and a durable
/// per-device token the phone stores and sends on later requests. The token is
/// returned only here and is never included in device listings.
/// </summary>
/// <param name="Device">The newly trusted device.</param>
/// <param name="Token">The device's authentication token.</param>
public sealed record ConfirmCompanionPairingResponse(
    CompanionDeviceDto Device,
    string Token);
