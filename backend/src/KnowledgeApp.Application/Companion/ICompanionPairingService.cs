using KnowledgeApp.Application.Common.Results;
using KnowledgeApp.Contracts.Companion;

namespace KnowledgeApp.Application.Companion;

/// <summary>
/// Manages the Companion Mode pairing lifecycle (the QR code) and the list of
/// trusted devices. Pairing sessions are short-lived and held in memory; the
/// actual phone-over-network handshake arrives with the local-network transport.
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
    /// Completes a pairing session for the given token and registers the device as
    /// trusted. Fails when no matching, unexpired session is active.
    /// </summary>
    Result<CompanionDeviceDto> Confirm(ConfirmCompanionPairingRequest request);

    /// <summary>Lists the currently trusted devices.</summary>
    CompanionDevicesResponse GetDevices();

    /// <summary>Removes a trusted device. Fails when the device is unknown.</summary>
    Result RevokeDevice(Guid deviceId);

    /// <summary>Updates a trusted device's permissions. Fails when the device is unknown.</summary>
    Result UpdateDevicePermissions(Guid deviceId, CompanionDevicePermissionsDto permissions);
}
