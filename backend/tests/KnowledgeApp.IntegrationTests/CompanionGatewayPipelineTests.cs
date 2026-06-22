using System.Net;
using KnowledgeApp.Application.Common.Results;
using KnowledgeApp.Application.Companion;
using KnowledgeApp.Contracts.Companion;
using KnowledgeApp.LocalApi.CompanionGateway;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;

namespace KnowledgeApp.IntegrationTests;

public sealed class CompanionGatewayPipelineTests : IAsyncLifetime
{
    private const string ValidToken = "good-token";

    private WebApplication app = null!;
    private HttpClient client = null!;
    private string spaDir = null!;
    private FakeForwarder forwarder = null!;

    public async Task InitializeAsync()
    {
        spaDir = Path.Combine(Path.GetTempPath(), "localmind-gateway-spa", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(spaDir);
        await File.WriteAllTextAsync(Path.Combine(spaDir, "index.html"), "<html>SPA</html>");

        forwarder = new FakeForwarder();
        var pairing = new FakePairingService(ValidToken, new CompanionDevicePermissionsDto(
            Chat: true,
            Search: false,
            ViewDocuments: false,
            ViewStatus: false,
            Rescan: false,
            AddFiles: false));

        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Logging.ClearProviders();
        builder.Services.AddRouting();

        app = builder.Build();
        CompanionGatewayPipeline.Configure(app, pairing, forwarder, new PhysicalFileProvider(spaDir));
        await app.StartAsync();
        client = app.GetTestClient();
    }

    public async Task DisposeAsync()
    {
        client.Dispose();
        await app.DisposeAsync();
        Directory.Delete(spaDir, recursive: true);
    }

    [Fact]
    public async Task Serves_The_Spa_At_Root_And_For_Spa_Routes()
    {
        Assert.Contains("SPA", await client.GetStringAsync("/"));
        Assert.Contains("SPA", await client.GetStringAsync("/companion"));
    }

    [Fact]
    public async Task Rejects_Api_Requests_Without_A_Token()
    {
        using HttpResponseMessage response = await client.GetAsync("/api/v1/chats");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.False(forwarder.WasCalled);
    }

    [Fact]
    public async Task Forwards_Permitted_Requests_For_A_Trusted_Device()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/chats");
        request.Headers.Add("Authorization", $"Bearer {ValidToken}");

        using HttpResponseMessage response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(forwarder.WasCalled);
    }

    [Fact]
    public async Task Rejects_Requests_For_A_Capability_The_Device_Lacks()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/companion/activity");
        request.Headers.Add("Authorization", $"Bearer {ValidToken}");

        using HttpResponseMessage response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.False(forwarder.WasCalled);
    }

    [Fact]
    public async Task Blocks_Non_Allowlisted_Routes()
    {
        using var request = new HttpRequestMessage(HttpMethod.Delete, "/api/v1/documents/abc");
        request.Headers.Add("Authorization", $"Bearer {ValidToken}");

        using HttpResponseMessage response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.False(forwarder.WasCalled);
    }

    [Fact]
    public async Task Allows_Pairing_Confirm_Without_A_Token()
    {
        using HttpResponseMessage response = await client.PostAsync(
            "/api/v1/companion/pairing/confirm",
            content: null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(forwarder.WasCalled);
    }

    private sealed class FakeForwarder : ICompanionForwarder
    {
        public bool WasCalled { get; private set; }

        public async Task ForwardAsync(HttpContext context, CancellationToken cancellationToken = default)
        {
            WasCalled = true;
            context.Response.StatusCode = StatusCodes.Status200OK;
            await context.Response.WriteAsync("FORWARDED", cancellationToken);
        }
    }

    private sealed class FakePairingService(string token, CompanionDevicePermissionsDto permissions)
        : ICompanionPairingService
    {
        private readonly CompanionDeviceDto device = new(
            Id: Guid.NewGuid(),
            Name: "Test Phone",
            Platform: "Test",
            CreatedAt: DateTimeOffset.UnixEpoch,
            LastSeenAt: DateTimeOffset.UnixEpoch,
            Permissions: permissions);

        public Task<CompanionDeviceDto?> FindByTokenAsync(
            string candidate,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(candidate == token ? device : null);

        public CompanionInfoDto GetInfo() => throw new NotSupportedException();

        public Task<Result<CompanionPairingSessionDto>> StartAsync(CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public CompanionPairingStatusDto GetStatus() => throw new NotSupportedException();

        public Result Cancel() => throw new NotSupportedException();

        public Task<Result<ConfirmCompanionPairingResponse>> ConfirmAsync(
            ConfirmCompanionPairingRequest request,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<CompanionDevicesResponse> GetDevicesAsync(CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<Result> RevokeDeviceAsync(Guid deviceId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<Result> UpdateDevicePermissionsAsync(
            Guid deviceId,
            CompanionDevicePermissionsDto value,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }
}
