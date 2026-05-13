using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace KnowledgeApp.IntegrationTests;

public sealed class DiagnosticsApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> factory;

    public DiagnosticsApiTests(WebApplicationFactory<Program> factory)
    {
        this.factory = factory;
    }

    [Fact]
    public async Task DiagnosticsEndpoint_Should_Return_Runtime_Diagnostics()
    {
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/api/diagnostics");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var diagnostics = await response.Content.ReadFromJsonAsync<DiagnosticsResponse>();

        Assert.NotNull(diagnostics);
        Assert.False(string.IsNullOrWhiteSpace(diagnostics.Paths.DatabasePath));
        Assert.False(string.IsNullOrWhiteSpace(diagnostics.Paths.FilesPath));
        Assert.False(string.IsNullOrWhiteSpace(diagnostics.Paths.IndexPath));
        Assert.False(string.IsNullOrWhiteSpace(diagnostics.Paths.LogsPath));
        Assert.False(string.IsNullOrWhiteSpace(diagnostics.RuntimeMode));
        Assert.False(string.IsNullOrWhiteSpace(diagnostics.LocalApiVersion));
        Assert.NotNull(diagnostics.AiRuntimeStatus);
        Assert.True(diagnostics.PendingIngestionJobsCount >= 0);
    }

    private sealed record DiagnosticsResponse(
        DiagnosticsPathsResponse Paths,
        string RuntimeMode,
        string LocalApiVersion,
        RuntimeStatusResponse AiRuntimeStatus,
        int PendingIngestionJobsCount);

    private sealed record DiagnosticsPathsResponse(
        string DatabasePath,
        string FilesPath,
        string IndexPath,
        string LogsPath);

    private sealed record RuntimeStatusResponse(
        bool LocalApiReady,
        string AiRuntimeStatus,
        bool ModelsAvailable,
        bool OfflineMode);
}
