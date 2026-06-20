using System.Net;
using System.Net.Http.Json;
using KnowledgeApp.Contracts.Common;
using KnowledgeApp.Contracts.Companion;
using KnowledgeApp.Contracts.Settings;

namespace KnowledgeApp.IntegrationTests;

public sealed class CompanionEndpointsTests : IClassFixture<LocalApiTestFactory>
{
    private readonly LocalApiTestFactory factory;

    public CompanionEndpointsTests(LocalApiTestFactory factory)
    {
        this.factory = factory;
    }

    [Fact]
    public async Task GetInfo_Should_Return_Computer_Name()
    {
        using HttpClient client = factory.CreateClient();

        CompanionInfoDto? info =
            await client.GetApiDataAsync<CompanionInfoDto>("/api/v1/companion/info");

        Assert.NotNull(info);
        Assert.False(string.IsNullOrWhiteSpace(info.ComputerName));
    }

    [Fact]
    public async Task StartPairing_Should_Fail_When_Companion_Mode_Disabled()
    {
        using HttpClient client = factory.CreateClient();
        await SetCompanionEnabledAsync(client, enabled: false);

        using HttpResponseMessage response = await client.PostAsync("/api/v1/companion/pairing", null);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        ApiResponse<object?> envelope = await response.Content.ReadApiErrorAsync();
        Assert.Equal("COMPANION_MODE_DISABLED", envelope.Error!.Code);
    }

    [Fact]
    public async Task StartPairing_Should_Return_Session_When_Enabled()
    {
        using HttpClient client = factory.CreateClient();
        await SetCompanionEnabledAsync(client, enabled: true);

        using HttpResponseMessage response = await client.PostAsync("/api/v1/companion/pairing", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        CompanionPairingSessionDto? session =
            await response.Content.ReadApiDataAsync<CompanionPairingSessionDto>();

        Assert.NotNull(session);
        Assert.False(string.IsNullOrWhiteSpace(session.Token));
        Assert.Equal(300, session.ExpiresInSeconds);
        Assert.Contains("/companion/pair", session.PairingUrl);
    }

    [Fact]
    public async Task Confirm_Then_Revoke_Should_Manage_Trusted_Device()
    {
        using HttpClient client = factory.CreateClient();
        await SetCompanionEnabledAsync(client, enabled: true);

        using HttpResponseMessage startResponse = await client.PostAsync("/api/v1/companion/pairing", null);
        CompanionPairingSessionDto? session =
            await startResponse.Content.ReadApiDataAsync<CompanionPairingSessionDto>();
        Assert.NotNull(session);

        using HttpResponseMessage confirmResponse = await client.PostAsJsonAsync(
            "/api/v1/companion/pairing/confirm",
            new ConfirmCompanionPairingRequest(session.Token, "Redmi Note", "Chrome"));

        Assert.Equal(HttpStatusCode.OK, confirmResponse.StatusCode);
        CompanionDeviceDto? device =
            await confirmResponse.Content.ReadApiDataAsync<CompanionDeviceDto>();
        Assert.NotNull(device);
        Assert.Equal("Redmi Note", device.Name);

        CompanionDevicesResponse? listed =
            await client.GetApiDataAsync<CompanionDevicesResponse>("/api/v1/companion/devices");
        Assert.NotNull(listed);
        Assert.Contains(listed.Devices, item => item.Id == device.Id);

        using HttpResponseMessage revokeResponse = await client.DeleteAsync(
            $"/api/v1/companion/devices/{device.Id}");
        Assert.Equal(HttpStatusCode.OK, revokeResponse.StatusCode);

        CompanionDevicesResponse? afterRevoke =
            await client.GetApiDataAsync<CompanionDevicesResponse>("/api/v1/companion/devices");
        Assert.NotNull(afterRevoke);
        Assert.DoesNotContain(afterRevoke.Devices, item => item.Id == device.Id);
    }

    private static async Task SetCompanionEnabledAsync(HttpClient client, bool enabled)
    {
        AppSettingsDto? settings = await client.GetApiDataAsync<AppSettingsDto>("/api/v1/settings");
        Assert.NotNull(settings);

        AppSettingsDto request = settings with
        {
            // The seeded test provider may not be a recognized value; pin a valid
            // one so the only change under test is Companion Mode.
            Ai = settings.Ai with { Provider = "LlamaCpp" },
            CompanionMode = new CompanionModeSettingsDto(enabled),
        };

        using HttpResponseMessage response = await client.PutAsJsonAsync("/api/v1/settings", request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
