using KnowledgeApp.Application.Abstractions;
using KnowledgeApp.Application.Common.Errors;
using KnowledgeApp.Application.Common.Results;
using KnowledgeApp.Application.Companion;
using KnowledgeApp.Application.Settings;
using KnowledgeApp.Contracts.Companion;
using KnowledgeApp.Contracts.Settings;
using KnowledgeApp.Domain.Entities;
using Microsoft.Extensions.DependencyInjection;

namespace KnowledgeApp.UnitTests.Companion;

public sealed class CompanionPairingServiceTests
{
    [Fact]
    public async Task StartAsync_Should_Fail_When_Companion_Mode_Disabled()
    {
        CompanionPairingService service = CreateService(companionEnabled: false);

        Result<CompanionPairingSessionDto> result = await service.StartAsync();

        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);
        Assert.Equal(ErrorCodes.Companion.ModeDisabled, result.Error.Code);
        Assert.Equal(ErrorType.Conflict, result.Error.Type);
    }

    [Fact]
    public async Task StartAsync_Should_Return_Session_When_Enabled()
    {
        FixedDateTimeProvider clock = new();
        CompanionPairingService service = CreateService(companionEnabled: true);

        Result<CompanionPairingSessionDto> result = await service.StartAsync();

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.False(string.IsNullOrWhiteSpace(result.Value.Token));
        Assert.Equal(300, result.Value.ExpiresInSeconds);
        Assert.Equal(clock.UtcNow.AddSeconds(300), result.Value.ExpiresAt);
        Assert.Contains(result.Value.Token, result.Value.PairingUrl);
        Assert.Contains("49322", result.Value.PairingUrl);
    }

    [Fact]
    public void GetInfo_Should_Return_Computer_Name()
    {
        CompanionPairingService service = CreateService(companionEnabled: true);

        CompanionInfoDto info = service.GetInfo();

        Assert.Equal("Vurain-PC", info.ComputerName);
    }

    [Fact]
    public void GetStatus_Should_Be_Inactive_Initially()
    {
        CompanionPairingService service = CreateService(companionEnabled: true);

        CompanionPairingStatusDto status = service.GetStatus();

        Assert.False(status.Active);
        Assert.Equal(0, status.ExpiresInSeconds);
    }

    [Fact]
    public async Task Cancel_Should_Clear_Active_Session()
    {
        CompanionPairingService service = CreateService(companionEnabled: true);
        await service.StartAsync();

        Assert.True(service.GetStatus().Active);

        service.Cancel();

        Assert.False(service.GetStatus().Active);
    }

    [Fact]
    public async Task Confirm_Should_Register_Trusted_Device_With_Valid_Token()
    {
        CompanionPairingService service = CreateService(companionEnabled: true);
        Result<CompanionPairingSessionDto> session = await service.StartAsync();

        Result<ConfirmCompanionPairingResponse> result = await service.ConfirmAsync(
            new ConfirmCompanionPairingRequest(session.Value!.Token, "Redmi Note", "Chrome"));

        Assert.True(result.IsSuccess);
        Assert.Equal("Redmi Note", result.Value!.Device.Name);
        Assert.Equal("Chrome", result.Value.Device.Platform);
        Assert.False(string.IsNullOrWhiteSpace(result.Value.Token));

        CompanionDevicesResponse devices = await service.GetDevicesAsync();
        Assert.Contains(devices.Devices, device => device.Id == result.Value.Device.Id);

        // Session is single-use and consumed on success.
        Assert.False(service.GetStatus().Active);
    }

    [Fact]
    public async Task Confirm_Should_Grant_Safe_Default_Permissions()
    {
        CompanionPairingService service = CreateService(companionEnabled: true);
        Result<CompanionPairingSessionDto> session = await service.StartAsync();

        Result<ConfirmCompanionPairingResponse> result = await service.ConfirmAsync(
            new ConfirmCompanionPairingRequest(session.Value!.Token, "Redmi Note", "Chrome"));

        CompanionDevicePermissionsDto permissions = result.Value!.Device.Permissions;
        Assert.True(permissions.Chat);
        Assert.True(permissions.Search);
        Assert.True(permissions.ViewDocuments);
        Assert.True(permissions.ViewStatus);
        Assert.True(permissions.Rescan);
        Assert.True(permissions.AddFiles);
    }

    [Fact]
    public async Task UpdateDevicePermissions_Should_Change_Permissions()
    {
        CompanionPairingService service = CreateService(companionEnabled: true);
        Result<CompanionPairingSessionDto> session = await service.StartAsync();
        Result<ConfirmCompanionPairingResponse> device = await service.ConfirmAsync(
            new ConfirmCompanionPairingRequest(session.Value!.Token, "Redmi Note", "Chrome"));

        Result update = await service.UpdateDevicePermissionsAsync(
            device.Value!.Device.Id,
            new CompanionDevicePermissionsDto(
                Chat: true,
                Search: true,
                ViewDocuments: true,
                ViewStatus: true,
                Rescan: false,
                AddFiles: false));

        Assert.True(update.IsSuccess);

        CompanionDeviceDto stored = Assert.Single((await service.GetDevicesAsync()).Devices);
        Assert.False(stored.Permissions.Rescan);
        Assert.False(stored.Permissions.AddFiles);
        Assert.True(stored.Permissions.Chat);
    }

    [Fact]
    public async Task UpdateDevicePermissions_Should_Fail_When_Device_Unknown()
    {
        CompanionPairingService service = CreateService(companionEnabled: true);

        Result update = await service.UpdateDevicePermissionsAsync(
            Guid.NewGuid(),
            new CompanionDevicePermissionsDto(true, true, true, true, true, true));

        Assert.False(update.IsSuccess);
        Assert.Equal(ErrorCodes.Companion.DeviceNotFound, update.Error!.Code);
    }

    [Fact]
    public async Task Confirm_Should_Fail_With_Invalid_Token()
    {
        CompanionPairingService service = CreateService(companionEnabled: true);
        await service.StartAsync();

        Result<ConfirmCompanionPairingResponse> result = await service.ConfirmAsync(
            new ConfirmCompanionPairingRequest("not-the-token", "Phone", "Chrome"));

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorCodes.Companion.PairingNotActive, result.Error!.Code);
        Assert.Empty((await service.GetDevicesAsync()).Devices);
    }

    [Fact]
    public async Task Confirm_Should_Fail_When_Fields_Missing()
    {
        CompanionPairingService service = CreateService(companionEnabled: true);

        Result<ConfirmCompanionPairingResponse> result = await service.ConfirmAsync(
            new ConfirmCompanionPairingRequest(string.Empty, string.Empty, string.Empty));

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.Error!.Type);
    }

    [Fact]
    public async Task RevokeDevice_Should_Remove_Trusted_Device()
    {
        CompanionPairingService service = CreateService(companionEnabled: true);
        Result<CompanionPairingSessionDto> session = await service.StartAsync();
        Result<ConfirmCompanionPairingResponse> device = await service.ConfirmAsync(
            new ConfirmCompanionPairingRequest(session.Value!.Token, "Redmi Note", "Chrome"));

        Result revoke = await service.RevokeDeviceAsync(device.Value!.Device.Id);

        Assert.True(revoke.IsSuccess);
        Assert.Empty((await service.GetDevicesAsync()).Devices);
    }

    [Fact]
    public async Task FindByToken_Should_Resolve_Trusted_Device()
    {
        CompanionPairingService service = CreateService(companionEnabled: true);
        Result<CompanionPairingSessionDto> session = await service.StartAsync();
        Result<ConfirmCompanionPairingResponse> confirmed = await service.ConfirmAsync(
            new ConfirmCompanionPairingRequest(session.Value!.Token, "Redmi Note", "Chrome"));

        CompanionDeviceDto? found = await service.FindByTokenAsync(confirmed.Value!.Token);

        Assert.NotNull(found);
        Assert.Equal(confirmed.Value.Device.Id, found.Id);
    }

    [Fact]
    public async Task FindByToken_Should_Return_Null_For_Unknown_Token()
    {
        CompanionPairingService service = CreateService(companionEnabled: true);

        Assert.Null(await service.FindByTokenAsync("not-a-token"));
    }

    [Fact]
    public async Task FindByToken_Should_Return_Null_After_Revoke()
    {
        CompanionPairingService service = CreateService(companionEnabled: true);
        Result<CompanionPairingSessionDto> session = await service.StartAsync();
        Result<ConfirmCompanionPairingResponse> confirmed = await service.ConfirmAsync(
            new ConfirmCompanionPairingRequest(session.Value!.Token, "Redmi Note", "Chrome"));

        await service.RevokeDeviceAsync(confirmed.Value!.Device.Id);

        Assert.Null(await service.FindByTokenAsync(confirmed.Value.Token));
    }

    [Fact]
    public async Task RevokeDevice_Should_Fail_When_Device_Unknown()
    {
        CompanionPairingService service = CreateService(companionEnabled: true);

        Result revoke = await service.RevokeDeviceAsync(Guid.NewGuid());

        Assert.False(revoke.IsSuccess);
        Assert.Equal(ErrorCodes.Companion.DeviceNotFound, revoke.Error!.Code);
    }

    private static CompanionPairingService CreateService(bool companionEnabled)
    {
        AppSettingsDto settings = new(
            default!,
            default!,
            default!,
            default!,
            CompanionMode: new CompanionModeSettingsDto(companionEnabled));

        ServiceCollection services = new();
        services.AddScoped<ISettingsService>(_ => new FakeSettingsService(settings));
        // Singleton so the in-memory device store is shared across the per-operation scopes.
        services.AddSingleton<ICompanionDeviceRepository>(new FakeCompanionDeviceRepository());
        ServiceProvider provider = services.BuildServiceProvider();

        return new CompanionPairingService(
            provider.GetRequiredService<IServiceScopeFactory>(),
            new FixedDateTimeProvider(),
            new FakeNetworkAddressProvider(),
            new FakeDeviceIdentityProvider());
    }

    private sealed class FakeSettingsService(AppSettingsDto settings) : ISettingsService
    {
        public Task<AppSettingsDto> GetAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(settings);
        }

        public Task<Result> UpdateAsync(AppSettingsDto request, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Result.Success());
        }
    }

    private sealed class FakeNetworkAddressProvider : ILocalNetworkAddressProvider
    {
        public string? GetLocalNetworkAddress() => "192.168.1.50";
    }

    private sealed class FakeDeviceIdentityProvider : ILocalDeviceIdentityProvider
    {
        public LocalDeviceIdentity GetIdentity() => new("device-key", "Vurain-PC");
    }

    private sealed class FakeCompanionDeviceRepository : ICompanionDeviceRepository
    {
        private readonly List<CompanionDevice> devices = [];

        public Task AddAsync(CompanionDevice device, CancellationToken cancellationToken = default)
        {
            devices.Add(device);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<CompanionDevice>> ListAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<CompanionDevice>>(
                devices.OrderBy(device => device.CreatedAt).ToList());
        }

        public Task<CompanionDevice?> FindByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(devices.FirstOrDefault(device => device.TokenHash == tokenHash));
        }

        public Task<CompanionDevice?> GetAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(devices.FirstOrDefault(device => device.Id == id));
        }

        public Task<bool> RemoveAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(devices.RemoveAll(device => device.Id == id) > 0);
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }
}
