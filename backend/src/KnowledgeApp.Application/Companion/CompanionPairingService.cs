using System.Collections.Concurrent;
using System.Security.Cryptography;
using KnowledgeApp.Application.Abstractions;
using KnowledgeApp.Application.Common.Errors;
using KnowledgeApp.Application.Common.Results;
using KnowledgeApp.Application.Settings;
using KnowledgeApp.Contracts.Common;
using KnowledgeApp.Contracts.Companion;
using KnowledgeApp.Contracts.Settings;
using KnowledgeApp.Domain.Entities;
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

    private static readonly TimeSpan LastSeenThrottle = TimeSpan.FromMinutes(5);

    private readonly object gate = new();
    private readonly ConcurrentDictionary<Guid, DateTimeOffset> lastSeenTouched = new();
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

    public async Task<Result<ConfirmCompanionPairingResponse>> ConfirmAsync(
        ConfirmCompanionPairingRequest request,
        CancellationToken cancellationToken = default)
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

        // Validate and consume the single-use session atomically.
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

            session = null;
        }

        string deviceToken = GenerateToken();
        CompanionDevice entity = new()
        {
            Name = Truncate(name),
            Platform = Truncate(platform),
            TokenHash = CompanionTokenHasher.Hash(deviceToken),
            CreatedAt = now,
            LastSeenAt = now,
            CanChat = DefaultPermissions.Chat,
            CanSearch = DefaultPermissions.Search,
            CanViewDocuments = DefaultPermissions.ViewDocuments,
            CanViewStatus = DefaultPermissions.ViewStatus,
            CanRescan = DefaultPermissions.Rescan,
            CanAddFiles = DefaultPermissions.AddFiles,
        };

        using IServiceScope scope = scopeFactory.CreateScope();
        ICompanionDeviceRepository repository =
            scope.ServiceProvider.GetRequiredService<ICompanionDeviceRepository>();
        await repository.AddAsync(entity, cancellationToken);

        activityFeed?.Publish("device.connected", $"{entity.Name} connected");

        return Result<ConfirmCompanionPairingResponse>.Success(
            new ConfirmCompanionPairingResponse(ToDto(entity), deviceToken));
    }

    public async Task<CompanionDeviceDto?> FindByTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return null;
        }

        using IServiceScope scope = scopeFactory.CreateScope();
        ICompanionDeviceRepository repository =
            scope.ServiceProvider.GetRequiredService<ICompanionDeviceRepository>();

        CompanionDevice? entity = await repository.FindByTokenHashAsync(
            CompanionTokenHasher.Hash(token),
            cancellationToken);

        if (entity is null)
        {
            return null;
        }

        DateTimeOffset now = dateTimeProvider.UtcNow;
        if (ShouldTouchLastSeen(entity.Id, now))
        {
            entity.LastSeenAt = now;
            await repository.SaveChangesAsync(cancellationToken);
            lastSeenTouched[entity.Id] = now;
        }

        return ToDto(entity);
    }

    public async Task<CompanionDevicesResponse> GetDevicesAsync(CancellationToken cancellationToken = default)
    {
        using IServiceScope scope = scopeFactory.CreateScope();
        ICompanionDeviceRepository repository =
            scope.ServiceProvider.GetRequiredService<ICompanionDeviceRepository>();

        IReadOnlyList<CompanionDevice> entities = await repository.ListAsync(cancellationToken);
        return new CompanionDevicesResponse(entities.Select(ToDto).ToArray());
    }

    public async Task<Result> RevokeDeviceAsync(Guid deviceId, CancellationToken cancellationToken = default)
    {
        using IServiceScope scope = scopeFactory.CreateScope();
        ICompanionDeviceRepository repository =
            scope.ServiceProvider.GetRequiredService<ICompanionDeviceRepository>();

        CompanionDevice? entity = await repository.GetAsync(deviceId, cancellationToken);
        if (entity is null)
        {
            return Result.Failure(ApplicationErrors.NotFound(
                ErrorCodes.Companion.DeviceNotFound,
                "Device not found."));
        }

        await repository.RemoveAsync(deviceId, cancellationToken);
        lastSeenTouched.TryRemove(deviceId, out _);
        activityFeed?.Publish("device.disconnected", $"{entity.Name} disconnected");

        return Result.Success();
    }

    public async Task<Result> UpdateDevicePermissionsAsync(
        Guid deviceId,
        CompanionDevicePermissionsDto permissions,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(permissions);

        using IServiceScope scope = scopeFactory.CreateScope();
        ICompanionDeviceRepository repository =
            scope.ServiceProvider.GetRequiredService<ICompanionDeviceRepository>();

        CompanionDevice? entity = await repository.GetAsync(deviceId, cancellationToken);
        if (entity is null)
        {
            return Result.Failure(ApplicationErrors.NotFound(
                ErrorCodes.Companion.DeviceNotFound,
                "Device not found."));
        }

        entity.CanChat = permissions.Chat;
        entity.CanSearch = permissions.Search;
        entity.CanViewDocuments = permissions.ViewDocuments;
        entity.CanViewStatus = permissions.ViewStatus;
        entity.CanRescan = permissions.Rescan;
        entity.CanAddFiles = permissions.AddFiles;
        await repository.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    private bool ShouldTouchLastSeen(Guid deviceId, DateTimeOffset now)
    {
        return !lastSeenTouched.TryGetValue(deviceId, out DateTimeOffset last)
            || now - last > LastSeenThrottle;
    }

    private static CompanionDeviceDto ToDto(CompanionDevice entity)
    {
        return new CompanionDeviceDto(
            Id: entity.Id,
            Name: entity.Name,
            Platform: entity.Platform,
            CreatedAt: entity.CreatedAt,
            LastSeenAt: entity.LastSeenAt,
            Permissions: new CompanionDevicePermissionsDto(
                Chat: entity.CanChat,
                Search: entity.CanSearch,
                ViewDocuments: entity.CanViewDocuments,
                ViewStatus: entity.CanViewStatus,
                Rescan: entity.CanRescan,
                AddFiles: entity.CanAddFiles));
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
