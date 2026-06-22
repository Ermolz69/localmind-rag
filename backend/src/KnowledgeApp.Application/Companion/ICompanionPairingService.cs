using KnowledgeApp.Application.Common.Results;
using KnowledgeApp.Contracts.Companion;

namespace KnowledgeApp.Application.Companion;

/// <summary>
/// Manages the Companion Mode pairing lifecycle (the QR code) and trusted devices.
/// Pairing sessions are short-lived and held in memory; trusted devices are
/// persisted (with hashed tokens) so a paired phone reconnects across restarts.
/// </summary>
public interface ICompanionPairingService
{
    /// <summary>Returns lightweight info shown by the phone companion interface.</summary>
    CompanionInfoDto GetInfo();

    /// <summary>Starts a new pairing session. Fails when Companion Mode is disabled.</summary>
    Task<Result<CompanionPairingSessionDto>> StartAsync(CancellationToken cancellationToken = default);

    /// <summary>Returns the current pairing session status.</summary>
    CompanionPairingStatusDto GetStatus();

    /// <summary>Cancels any active pairing session. Idempotent.</summary>
    Result Cancel();

    /// <summary>
    /// Completes a pairing session for the given token and persists the device as
    /// trusted, returning the device and its durable per-device token (shown once).
    /// Fails when no matching, unexpired session is active.
    /// </summary>
    Task<Result<ConfirmCompanionPairingResponse>> ConfirmAsync(
        ConfirmCompanionPairingRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Resolves a trusted device (with its current permissions) from its per-device
    /// token, or <c>null</c> when the token is unknown. Used by the LAN gateway to
    /// authenticate phone requests.
    /// </summary>
    Task<CompanionDeviceDto?> FindByTokenAsync(string token, CancellationToken cancellationToken = default);

    /// <summary>Lists the trusted devices.</summary>
    Task<CompanionDevicesResponse> GetDevicesAsync(CancellationToken cancellationToken = default);

    /// <summary>Removes a trusted device. Fails when the device is unknown.</summary>
    Task<Result> RevokeDeviceAsync(Guid deviceId, CancellationToken cancellationToken = default);

    /// <summary>Updates a trusted device's permissions. Fails when the device is unknown.</summary>
    Task<Result> UpdateDevicePermissionsAsync(
        Guid deviceId,
        CompanionDevicePermissionsDto permissions,
        CancellationToken cancellationToken = default);
}
