using System.Security.Cryptography;
using KnowledgeApp.Application.Abstractions;
using KnowledgeApp.Application.Common.Errors;
using KnowledgeApp.Application.Common.Results;
using KnowledgeApp.Application.Settings;
using KnowledgeApp.Contracts.Common;
using KnowledgeApp.Contracts.Companion;
using KnowledgeApp.Contracts.Settings;
using Microsoft.Extensions.DependencyInjection;

namespace KnowledgeApp.Application.Companion;

/// <inheritdoc />
public sealed class CompanionPairingService(
    IServiceScopeFactory scopeFactory,
    IDateTimeProvider dateTimeProvider,
    ILocalNetworkAddressProvider networkAddressProvider,
    ILocalDeviceIdentityProvider deviceIdentityProvider,
    ICompanionActivityFeed? activityFeed = null) : ICompanionPairingService
{
    private const int PairingTtlSeconds = 300;

    // The local-network transport that will serve this URL lands in a later stage;
    // the port is reserved here so the QR payload is forward-compatible.
    private const int CompanionPort = 49322;

    private const int MaxDeviceFieldLength = 100;

    // Recommended safe default for a newly paired phone: useful capabilities,
    // none of the dangerous ones.
    private static readonly CompanionDevicePermissionsDto DefaultPermissions = new(
        Chat: true,
        Search: true,
        ViewDocuments: true,
        ViewStatus: true,
        Rescan: true,
        AddFiles: true);

    private readonly object gate = new();
    private readonly List<CompanionDeviceDto> devices = [];
    private readonly Dictionary<string, Guid> deviceIdByToken = new(StringComparer.Ordinal);
    private PairingState? session;

    public CompanionInfoDto GetInfo()
    {
        return new CompanionInfoDto(deviceIdentityProvider.GetIdentity().Name);
    }

    public async Task<Result<CompanionPairingSessionDto>> StartAsync(CancellationToken cancellationToken = default)
    {
        using IServiceScope scope = scopeFactory.CreateScope();
        ISettingsService settingsService = scope.ServiceProvider.GetRequiredService<ISettingsService>();
        AppSettingsDto settings = await settingsService.GetAsync(cancellationToken);

        if (!(settings.CompanionMode?.Enabled ?? false))
        {
            return Result<CompanionPairingSessionDto>.Failure(ApplicationErrors.Conflict(
                ErrorCodes.Companion.ModeDisabled,
                "Companion Mode is disabled. Enable it before pairing a phone."));
        }

        DateTimeOffset now = dateTimeProvider.UtcNow;
        DateTimeOffset expiresAt = now.AddSeconds(PairingTtlSeconds);
        string token = GenerateToken();
        string pairingUrl = BuildPairingUrl(token);

        lock (gate)
        {
            session = new PairingState(token, expiresAt);
        }

        return Result<CompanionPairingSessionDto>.Success(new CompanionPairingSessionDto(
            Token: token,
            PairingUrl: pairingUrl,
            ExpiresAt: expiresAt,
            ExpiresInSeconds: PairingTtlSeconds));
    }

    public CompanionPairingStatusDto GetStatus()
    {
        DateTimeOffset now = dateTimeProvider.UtcNow;

        lock (gate)
        {
            if (session is null || session.ExpiresAt <= now)
            {
                session = null;
                return new CompanionPairingStatusDto(Active: false, ExpiresAt: null, ExpiresInSeconds: 0);
            }

            int remaining = (int)Math.Ceiling((session.ExpiresAt - now).TotalSeconds);
            return new CompanionPairingStatusDto(Active: true, ExpiresAt: session.ExpiresAt, ExpiresInSeconds: remaining);
        }
    }

    public Result Cancel()
    {
        lock (gate)
        {
            session = null;
        }

        return Result.Success();
    }

    public Result<ConfirmCompanionPairingResponse> Confirm(ConfirmCompanionPairingRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        string name = (request.DeviceName ?? string.Empty).Trim();
        string platform = (request.Platform ?? string.Empty).Trim();

        Dictionary<string, string[]> errors = new();

        if (string.IsNullOrWhiteSpace(request.Token))
        {
            errors["token"] = [ErrorMessages.ValueRequired];
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            errors["deviceName"] = [ErrorMessages.ValueRequired];
        }

        if (string.IsNullOrWhiteSpace(platform))
        {
            errors["platform"] = [ErrorMessages.ValueRequired];
        }

        if (errors.Count > 0)
        {
            return Result<ConfirmCompanionPairingResponse>.Failure(ApplicationErrors.Validation(
                ErrorCodes.Companion.ValidationFailed,
                "Pairing confirmation is invalid.",
                errors));
        }

        DateTimeOffset now = dateTimeProvider.UtcNow;

        lock (gate)
        {
            if (session is null
                || session.ExpiresAt <= now
                || !CryptographicOperations.FixedTimeEquals(
                    System.Text.Encoding.UTF8.GetBytes(session.Token),
                    System.Text.Encoding.UTF8.GetBytes(request.Token)))
            {
                return Result<ConfirmCompanionPairingResponse>.Failure(ApplicationErrors.Conflict(
                    ErrorCodes.Companion.PairingNotActive,
                    "No active pairing session matches this code. Generate a new QR code and try again."));
            }

            // Single-use: consume the session on success.
            session = null;

            CompanionDeviceDto device = new(
                Id: Guid.NewGuid(),
                Name: Truncate(name),
                Platform: Truncate(platform),
                CreatedAt: now,
                LastSeenAt: now,
                Permissions: DefaultPermissions);

            string deviceToken = GenerateToken();
            devices.Add(device);
            deviceIdByToken[deviceToken] = device.Id;
            activityFeed?.Publish("device.connected", $"{device.Name} connected");

            return Result<ConfirmCompanionPairingResponse>.Success(
                new ConfirmCompanionPairingResponse(device, deviceToken));
        }
    }

    public CompanionDeviceDto? FindByToken(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return null;
        }

        lock (gate)
        {
            return deviceIdByToken.TryGetValue(token, out Guid deviceId)
                ? devices.Find(device => device.Id == deviceId)
                : null;
        }
    }

    public CompanionDevicesResponse GetDevices()
    {
        lock (gate)
        {
            return new CompanionDevicesResponse(devices.ToArray());
        }
    }

    public Result RevokeDevice(Guid deviceId)
    {
        string removedName;

        lock (gate)
        {
            CompanionDeviceDto? existing = devices.Find(device => device.Id == deviceId);

            if (existing is null)
            {
                return Result.Failure(ApplicationErrors.NotFound(
                    ErrorCodes.Companion.DeviceNotFound,
                    "Device not found."));
            }

            removedName = existing.Name;
            devices.RemoveAll(device => device.Id == deviceId);

            foreach (string token in deviceIdByToken
                .Where(entry => entry.Value == deviceId)
                .Select(entry => entry.Key)
                .ToArray())
            {
                deviceIdByToken.Remove(token);
            }
        }

        activityFeed?.Publish("device.disconnected", $"{removedName} disconnected");
        return Result.Success();
    }

    public Result UpdateDevicePermissions(Guid deviceId, CompanionDevicePermissionsDto permissions)
    {
        ArgumentNullException.ThrowIfNull(permissions);

        lock (gate)
        {
            int index = devices.FindIndex(device => device.Id == deviceId);

            if (index < 0)
            {
                return Result.Failure(ApplicationErrors.NotFound(
                    ErrorCodes.Companion.DeviceNotFound,
                    "Device not found."));
            }

            devices[index] = devices[index] with { Permissions = permissions };
        }

        return Result.Success();
    }

    private string BuildPairingUrl(string token)
    {
        string host = networkAddressProvider.GetLocalNetworkAddress() ?? "localhost";
        return $"http://{host}:{CompanionPort}/companion?token={token}";
    }

    private static string GenerateToken()
    {
        return Convert.ToHexString(RandomNumberGenerator.GetBytes(16)).ToLowerInvariant();
    }

    private static string Truncate(string value)
    {
        return value.Length <= MaxDeviceFieldLength ? value : value[..MaxDeviceFieldLength];
    }

    private sealed record PairingState(string Token, DateTimeOffset ExpiresAt);
}
