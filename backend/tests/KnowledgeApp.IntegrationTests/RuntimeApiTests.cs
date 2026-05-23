using System.Net;
using System.Net.Http.Json;
using KnowledgeApp.Application.Abstractions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace KnowledgeApp.IntegrationTests;

public sealed class RuntimeApiTests
{
    [Fact]
    public async Task RuntimeStatus_Should_Report_SetupRequired_When_AiAssets_Are_Missing()
    {
        using LocalApiTestFactory factory = new();
        using HttpClient client = factory.CreateClient();

        RuntimeStatusResponse? status = await client.GetFromJsonAsync<RuntimeStatusResponse>("/api/runtime/status");

        Assert.NotNull(status);
        Assert.True(status.SetupRequired);
        Assert.False(string.IsNullOrWhiteSpace(status.SetupReason));
        Assert.False(string.IsNullOrWhiteSpace(status.RuntimePath));
        Assert.False(string.IsNullOrWhiteSpace(status.ModelPath));
    }

    [Fact]
    public async Task RuntimeSetup_Should_Invoke_FirstRun_Setup_Service()
    {
        FakeRuntimeSetupService setup = new();
        using LocalApiTestFactory baseFactory = new();
        using WebApplicationFactory<Program> factory = baseFactory.WithWebHostBuilder(builder =>
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IAiRuntimeSetupService>();
                services.AddSingleton<IAiRuntimeSetupService>(setup);
            }));
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage response = await client.PostAsync("/api/runtime/ai/setup", content: null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(setup.WasCalled);

        RuntimeSetupResponse? setupResponse = await response.Content.ReadFromJsonAsync<RuntimeSetupResponse>();
        Assert.NotNull(setupResponse);
        Assert.NotNull(setupResponse.Status);
    }

    private sealed class FakeRuntimeSetupService : IAiRuntimeSetupService
    {
        public bool WasCalled { get; private set; }

        public Task SetupAsync(CancellationToken cancellationToken = default)
        {
            WasCalled = true;
            return Task.CompletedTask;
        }
    }

    private sealed record RuntimeSetupResponse(
        bool RuntimeInstalled,
        bool ModelInstalled,
        string Message,
        RuntimeStatusResponse Status);

    private sealed record RuntimeStatusResponse(
        bool LocalApiReady,
        string AiRuntimeStatus,
        bool ModelsAvailable,
        bool OfflineMode,
        string? RuntimePath,
        string? ModelPath,
        bool SetupRequired,
        string? SetupReason);
}
