using System.Net;
using System.Net.Http.Json;
using KnowledgeApp.Domain.Entities;
using KnowledgeApp.Domain.Enums;
using KnowledgeApp.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

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
        Assert.True(diagnostics.Storage.DatabaseSizeBytes >= 0);
        Assert.True(diagnostics.Storage.FilesSizeBytes >= 0);
        Assert.True(diagnostics.Storage.IndexSizeBytes >= 0);
        Assert.True(diagnostics.Storage.LogsSizeBytes >= 0);
        Assert.True(diagnostics.Counts.PendingIngestionJobsCount >= 0);
        Assert.True(diagnostics.Counts.FailedIngestionJobsCount >= 0);
        Assert.False(string.IsNullOrWhiteSpace(diagnostics.Runtime.RuntimeMode));
        Assert.False(string.IsNullOrWhiteSpace(diagnostics.Runtime.LocalApiVersion));
        Assert.NotNull(diagnostics.Runtime.AiRuntimeStatus);
    }

    [Fact]
    public async Task DiagnosticsEndpoint_Should_Return_Counts_And_Latest_Errors()
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var document = new Document
        {
            Name = $"failed-{Guid.NewGuid():N}.pdf",
            Status = DocumentStatus.Failed,
        };
        var failedJob = new IngestionJob
        {
            DocumentId = document.Id,
            LastError = "PDF file signature is invalid.",
            ProcessedAt = DateTimeOffset.UtcNow,
            Status = IngestionJobStatus.Failed,
        };
        db.Documents.Add(document);
        db.IngestionJobs.Add(failedJob);
        await db.SaveChangesAsync();

        using var client = factory.CreateClient();

        var diagnostics = await client.GetFromJsonAsync<DiagnosticsResponse>("/api/diagnostics");

        Assert.NotNull(diagnostics);
        Assert.True(diagnostics.Counts.DocumentsCount >= 1);
        Assert.True(diagnostics.Counts.FailedIngestionJobsCount >= 1);
        Assert.Contains(diagnostics.LatestErrors, error =>
            error.JobId == failedJob.Id &&
            error.DocumentId == document.Id &&
            error.DocumentName == document.Name &&
            error.LastError == failedJob.LastError);
    }

    private sealed record DiagnosticsResponse(
        DiagnosticsPathsResponse Paths,
        DiagnosticsStorageResponse Storage,
        DiagnosticsCountsResponse Counts,
        IReadOnlyList<DiagnosticsIngestionErrorResponse> LatestErrors,
        DiagnosticsRuntimeResponse Runtime);

    private sealed record DiagnosticsPathsResponse(
        string DatabasePath,
        string FilesPath,
        string IndexPath,
        string LogsPath);

    private sealed record DiagnosticsStorageResponse(
        long DatabaseSizeBytes,
        long FilesSizeBytes,
        long IndexSizeBytes,
        long LogsSizeBytes);

    private sealed record DiagnosticsCountsResponse(
        int BucketsCount,
        int DocumentsCount,
        int DocumentFilesCount,
        int DocumentChunksCount,
        int DocumentEmbeddingsCount,
        int NotesCount,
        int ConversationsCount,
        int PendingIngestionJobsCount,
        int FailedIngestionJobsCount);

    private sealed record DiagnosticsIngestionErrorResponse(
        Guid JobId,
        Guid DocumentId,
        string DocumentName,
        string LastError,
        DateTimeOffset? ProcessedAt);

    private sealed record DiagnosticsRuntimeResponse(
        string RuntimeMode,
        string LocalApiVersion,
        RuntimeStatusResponse AiRuntimeStatus);

    private sealed record RuntimeStatusResponse(
        bool LocalApiReady,
        string AiRuntimeStatus,
        bool ModelsAvailable,
        bool OfflineMode);
}
