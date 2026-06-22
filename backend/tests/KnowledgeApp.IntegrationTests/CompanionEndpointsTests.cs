using System.Net;
using System.Net.Http.Json;
using KnowledgeApp.Contracts.Common;
using KnowledgeApp.Contracts.Companion;
using KnowledgeApp.Contracts.Settings;
using KnowledgeApp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

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
        Assert.Contains("/companion?token=", session.PairingUrl);
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
        ConfirmCompanionPairingResponse? confirmed =
            await confirmResponse.Content.ReadApiDataAsync<ConfirmCompanionPairingResponse>();
        Assert.NotNull(confirmed);
        Assert.Equal("Redmi Note", confirmed.Device.Name);
        Assert.False(string.IsNullOrWhiteSpace(confirmed.Token));
        CompanionDeviceDto device = confirmed.Device;

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

    [Fact]
    public async Task Confirmed_Device_Is_Persisted_With_A_Hashed_Token()
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
        ConfirmCompanionPairingResponse? confirmed =
            await confirmResponse.Content.ReadApiDataAsync<ConfirmCompanionPairingResponse>();
        Assert.NotNull(confirmed);

        using IServiceScope scope = factory.Services.CreateScope();
        AppDbContext db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var row = await db.CompanionDevices.FirstOrDefaultAsync(
            device => device.Id == confirmed.Device.Id);

        Assert.NotNull(row);
        Assert.False(string.IsNullOrWhiteSpace(row.TokenHash));
        Assert.NotEqual(confirmed.Token, row.TokenHash); // stored as a hash, not plaintext
        Assert.True(row.CanChat);
    }

    [Fact]
    public async Task FileBrowsing_Should_List_And_Add_Files_Within_Allowed_Roots()
    {
        string allowedRoot = Path.Combine(
            Path.GetTempPath(),
            "localmind-companion-files",
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(allowedRoot);
        string filePath = Path.Combine(allowedRoot, "notes.txt");
        await File.WriteAllTextAsync(filePath, "Companion file contents.");

        try
        {
            using HttpClient client = factory.CreateClient();
            await SetCompanionEnabledAsync(client, enabled: true, allowedFolders: [allowedRoot]);

            CompanionRootsResponse? roots =
                await client.GetApiDataAsync<CompanionRootsResponse>("/api/v1/companion/files/roots");
            Assert.NotNull(roots);
            Assert.Contains(roots.Roots, root => root.Path == allowedRoot);

            CompanionBrowseResponse? browse = await client.GetApiDataAsync<CompanionBrowseResponse>(
                $"/api/v1/companion/files/browse?path={Uri.EscapeDataString(allowedRoot)}");
            Assert.NotNull(browse);
            Assert.Contains(browse.Entries, entry => entry.Name == "notes.txt" && !entry.IsDirectory);

            using HttpResponseMessage addResponse = await client.PostAsJsonAsync(
                "/api/v1/companion/files/add",
                new AddCompanionFileRequest(filePath));
            Assert.Equal(HttpStatusCode.OK, addResponse.StatusCode);
        }
        finally
        {
            Directory.Delete(allowedRoot, recursive: true);
        }
    }

    [Fact]
    public async Task Adding_A_File_Should_Appear_In_The_Activity_Feed()
    {
        string allowedRoot = Path.Combine(
            Path.GetTempPath(),
            "localmind-companion-activity",
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(allowedRoot);
        string fileName = $"activity-{Guid.NewGuid():N}.txt";
        string filePath = Path.Combine(allowedRoot, fileName);
        await File.WriteAllTextAsync(filePath, "Activity feed contents.");

        try
        {
            using HttpClient client = factory.CreateClient();
            await SetCompanionEnabledAsync(client, enabled: true, allowedFolders: [allowedRoot]);

            using HttpResponseMessage addResponse = await client.PostAsJsonAsync(
                "/api/v1/companion/files/add",
                new AddCompanionFileRequest(filePath));
            Assert.Equal(HttpStatusCode.OK, addResponse.StatusCode);

            CompanionActivityResponse? activity =
                await client.GetApiDataAsync<CompanionActivityResponse>("/api/v1/companion/activity");
            Assert.NotNull(activity);
            Assert.Contains(
                activity.Events,
                item => item.Kind == "document.added" && item.Message.Contains(fileName));
        }
        finally
        {
            Directory.Delete(allowedRoot, recursive: true);
        }
    }

    [Fact]
    public async Task Browse_Should_Reject_Path_Outside_Allowed_Roots()
    {
        string allowedRoot = Path.Combine(
            Path.GetTempPath(),
            "localmind-companion-allowed",
            Guid.NewGuid().ToString("N"));
        string outsideRoot = Path.Combine(
            Path.GetTempPath(),
            "localmind-companion-outside",
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(allowedRoot);
        Directory.CreateDirectory(outsideRoot);

        try
        {
            using HttpClient client = factory.CreateClient();
            await SetCompanionEnabledAsync(client, enabled: true, allowedFolders: [allowedRoot]);

            using HttpResponseMessage response = await client.GetAsync(
                $"/api/v1/companion/files/browse?path={Uri.EscapeDataString(outsideRoot)}");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }
        finally
        {
            Directory.Delete(allowedRoot, recursive: true);
            Directory.Delete(outsideRoot, recursive: true);
        }
    }

    private static async Task SetCompanionEnabledAsync(
        HttpClient client,
        bool enabled,
        string[]? allowedFolders = null)
    {
        AppSettingsDto? settings = await client.GetApiDataAsync<AppSettingsDto>("/api/v1/settings");
        Assert.NotNull(settings);

        AppSettingsDto request = settings with
        {
            // The seeded test provider may not be a recognized value; pin a valid
            // one so the only change under test is Companion Mode.
            Ai = settings.Ai with { Provider = "LlamaCpp" },
            CompanionMode = new CompanionModeSettingsDto(enabled, allowedFolders ?? []),
        };

        using HttpResponseMessage response = await client.PutAsJsonAsync("/api/v1/settings", request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
