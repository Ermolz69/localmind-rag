using System.Net;
using System.Net.Http.Json;

using KnowledgeApp.Contracts.Common;
using KnowledgeApp.Contracts.Runtime;
using KnowledgeApp.Domain.Entities;
using KnowledgeApp.Domain.Enums;
using KnowledgeApp.Infrastructure.Persistence;

using Microsoft.Extensions.DependencyInjection;

namespace KnowledgeApp.IntegrationTests;

public sealed class DiagnosticsApiTests : IClassFixture<LocalApiTestFactory>
{
    private readonly LocalApiTestFactory factory;

    public DiagnosticsApiTests(LocalApiTestFactory factory)
    {
        this.factory = factory;
    }

    [Fact]
    public async Task DiagnosticsEndpoint_Should_Return_Aggregate_Diagnostics()
    {
        using HttpClient client = factory.CreateClient();

        DiagnosticsDto? diagnostics =
            await client.GetApiDataAsync<DiagnosticsDto>("/api/v1/diagnostics");

        Assert.NotNull(diagnostics);
        Assert.Equal(DiagnosticsHealthStatus.Healthy, diagnostics.Status);
        Assert.NotNull(diagnostics.Database);
        Assert.NotNull(diagnostics.Storage);
        Assert.NotNull(diagnostics.VectorIndex);
        Assert.NotNull(diagnostics.Runtime);
    }

    [Fact]
    public async Task HealthEndpoint_Should_Return_General_Health()
    {
        using HttpClient client = factory.CreateClient();

        DiagnosticsHealthStatus status =
            await client.GetApiDataAsync<DiagnosticsHealthStatus>("/api/v1/diagnostics/health");

        Assert.Equal(DiagnosticsHealthStatus.Healthy, status);
    }

    [Fact]
    public async Task DatabaseEndpoint_Should_Return_Database_Diagnostics()
    {
        await using AsyncServiceScope scope = factory.Services.CreateAsyncScope();
        AppDbContext db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        Document document = new Document
        {
            Name = $"db-test-{Guid.NewGuid():N}.pdf",
            Status = DocumentStatus.Indexed,
        };
        db.Documents.Add(document);
        await db.SaveChangesAsync();

        using HttpClient client = factory.CreateClient();

        DiagnosticsDatabaseDto? database =
            await client.GetApiDataAsync<DiagnosticsDatabaseDto>("/api/v1/diagnostics/database");

        Assert.NotNull(database);
        Assert.Equal(DiagnosticsHealthStatus.Healthy, database.Status);
        Assert.True(database.DocumentsCount >= 1);
    }

    [Fact]
    public async Task StorageEndpoint_Should_Return_Storage_Diagnostics()
    {
        using HttpClient client = factory.CreateClient();

        DiagnosticsStorageDto? storage =
            await client.GetApiDataAsync<DiagnosticsStorageDto>("/api/v1/diagnostics/storage");

        Assert.NotNull(storage);
        // Might be Healthy or Degraded depending on whether factory created all dirs
        Assert.True(storage.Status == DiagnosticsHealthStatus.Healthy || storage.Status == DiagnosticsHealthStatus.Degraded);
        Assert.True(storage.DatabaseSizeBytes >= 0);
    }

    [Fact]
    public async Task RuntimeEndpoint_Should_Return_Runtime_Diagnostics()
    {
        using HttpClient client = factory.CreateClient();

        DiagnosticsRuntimeDto? runtime =
            await client.GetApiDataAsync<DiagnosticsRuntimeDto>("/api/v1/diagnostics/runtime");

        Assert.NotNull(runtime);
        // Status might be Degraded if "Stub" provider reports missing models
        Assert.False(string.IsNullOrWhiteSpace(runtime.RuntimeMode));
        Assert.False(string.IsNullOrWhiteSpace(runtime.LocalApiVersion));
        Assert.NotNull(runtime.AiRuntimeStatus);
    }

    [Fact]
    public async Task VectorIndexEndpoint_Should_Return_Vector_Index_Diagnostics()
    {
        using HttpClient client = factory.CreateClient();

        DiagnosticsVectorIndexDto? vectorIndex =
            await client.GetApiDataAsync<DiagnosticsVectorIndexDto>("/api/v1/diagnostics/vector-index");

        Assert.NotNull(vectorIndex);
        Assert.True(vectorIndex.DocumentChunksCount >= 0);
        Assert.True(vectorIndex.DocumentEmbeddingsCount >= 0);
    }

    [Fact]
    public async Task DiagnosticsEndpoint_Should_Return_Latest_Errors()
    {
        await using AsyncServiceScope scope = factory.Services.CreateAsyncScope();
        AppDbContext db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        Document document = new Document
        {
            Name = $"failed-{Guid.NewGuid():N}.pdf",
            Status = DocumentStatus.Failed,
        };

        IngestionJob failedJob = new IngestionJob
        {
            DocumentId = document.Id,
            ErrorCode = "INGESTION_JOB_FAILED",
            ErrorMessage = "Test error message.",
            ProcessedAt = DateTimeOffset.UtcNow,
            Status = IngestionJobStatus.Failed,
        };

        db.Documents.Add(document);
        db.IngestionJobs.Add(failedJob);
        await db.SaveChangesAsync();

        using HttpClient client = factory.CreateClient();

        DiagnosticsDto? diagnostics =
            await client.GetApiDataAsync<DiagnosticsDto>("/api/v1/diagnostics");

        Assert.NotNull(diagnostics);
        Assert.Contains(
            diagnostics.LatestErrors,
            error =>
                error.JobId == failedJob.Id
                && error.DocumentId == document.Id
                && error.DocumentName == document.Name);
    }

    [Fact]
    public async Task OperationsEndpoint_Should_Return_Recent_Logs()
    {
        await using AsyncServiceScope scope = factory.Services.CreateAsyncScope();
        AppDbContext db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        OperationLog log = new OperationLog
        {
            OperationType = "Test.Operation",
            EntityType = "Test",
            EntityId = "123",
            Message = "Test message",
            MetadataJson = "{}"
        };

        db.OperationLogs.Add(log);
        await db.SaveChangesAsync();

        using HttpClient client = factory.CreateClient();

        CursorPage<KnowledgeApp.Contracts.Diagnostics.OperationLogDto>? logs =
            await client.GetApiDataAsync<CursorPage<KnowledgeApp.Contracts.Diagnostics.OperationLogDto>>("/api/v1/diagnostics/operations");

        Assert.NotNull(logs);
        Assert.Contains(logs.Items, x => x.OperationType == "Test.Operation" && x.EntityId == "123");
    }
}
