using KnowledgeApp.Application.Abstractions;
using KnowledgeApp.Application.Common.Errors;
using KnowledgeApp.Application.Common.Results;
using KnowledgeApp.Application.Companion;
using KnowledgeApp.Application.Settings;
using KnowledgeApp.Contracts.Companion;
using KnowledgeApp.Contracts.Settings;
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

        Result<CompanionDeviceDto> result = service.Confirm(
            new ConfirmCompanionPairingRequest(session.Value!.Token, "Redmi Note", "Chrome"));

        Assert.True(result.IsSuccess);
        Assert.Equal("Redmi Note", result.Value!.Name);
        Assert.Equal("Chrome", result.Value.Platform);

        Assert.Contains(service.GetDevices().Devices, device => device.Id == result.Value.Id);

        // Session is single-use and consumed on success.
        Assert.False(service.GetStatus().Active);
    }

    [Fact]
    public async Task Confirm_Should_Fail_With_Invalid_Token()
    {
        CompanionPairingService service = CreateService(companionEnabled: true);
        await service.StartAsync();

        Result<CompanionDeviceDto> result = service.Confirm(
            new ConfirmCompanionPairingRequest("not-the-token", "Phone", "Chrome"));

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorCodes.Companion.PairingNotActive, result.Error!.Code);
        Assert.Empty(service.GetDevices().Devices);
    }

    [Fact]
    public void Confirm_Should_Fail_When_Fields_Missing()
    {
        CompanionPairingService service = CreateService(companionEnabled: true);

        Result<CompanionDeviceDto> result = service.Confirm(
            new ConfirmCompanionPairingRequest(string.Empty, string.Empty, string.Empty));

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.Error!.Type);
    }

    [Fact]
    public async Task RevokeDevice_Should_Remove_Trusted_Device()
    {
        CompanionPairingService service = CreateService(companionEnabled: true);
        Result<CompanionPairingSessionDto> session = await service.StartAsync();
        Result<CompanionDeviceDto> device = service.Confirm(
            new ConfirmCompanionPairingRequest(session.Value!.Token, "Redmi Note", "Chrome"));

        Result revoke = service.RevokeDevice(device.Value!.Id);

        Assert.True(revoke.IsSuccess);
        Assert.Empty(service.GetDevices().Devices);
    }

    [Fact]
    public void RevokeDevice_Should_Fail_When_Device_Unknown()
    {
        CompanionPairingService service = CreateService(companionEnabled: true);

        Result revoke = service.RevokeDevice(Guid.NewGuid());

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
}
