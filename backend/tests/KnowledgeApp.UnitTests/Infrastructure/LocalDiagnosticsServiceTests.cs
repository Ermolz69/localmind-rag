using KnowledgeApp.Application.Abstractions;
using KnowledgeApp.Contracts.Runtime;
using KnowledgeApp.Domain.Entities;
using KnowledgeApp.Domain.Enums;
using KnowledgeApp.Infrastructure.Options;
using KnowledgeApp.Infrastructure.Persistence;
using KnowledgeApp.Infrastructure.Services;
using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeApp.UnitTests.Infrastructure;

public sealed class LocalDiagnosticsServiceTests
{
    [Fact]
    public void SafeDirectorySize_Should_Return_Zero_When_Directory_Is_Missing()
    {
        string? missingPath = Path.Combine(Path.GetTempPath(), $"localmind-missing-{Guid.NewGuid():N}");

        long size = LocalDiagnosticsService.SafeDirectorySize(missingPath);

        Assert.Equal(0, size);
    }

    [Fact]
    public async Task SafeDirectorySize_Should_Return_Total_File_Size()
    {
        DirectoryInfo? directory = Directory.CreateTempSubdirectory("localmind-diagnostics-");
        try
        {
            await File.WriteAllBytesAsync(Path.Combine(directory.FullName, "a.bin"), new byte[3]);
            DirectoryInfo? nested = Directory.CreateDirectory(Path.Combine(directory.FullName, "nested"));
            await File.WriteAllBytesAsync(Path.Combine(nested.FullName, "b.bin"), new byte[5]);

            long size = LocalDiagnosticsService.SafeDirectorySize(directory.FullName);

            Assert.Equal(8, size);
        }
        finally
        {
            directory.Delete(recursive: true);
        }
    }

    [Fact]
    public async Task GetDatabaseAsync_Should_Return_Healthy_When_Connected()
    {
        await using ApplicationTestDatabase database = await ApplicationTestDatabase.CreateAsync();
        LocalDiagnosticsService service = CreateService(database.Context);

        DiagnosticsDatabaseDto result = await service.GetDatabaseAsync();

        Assert.Equal(DiagnosticsHealthStatus.Healthy, result.Status);
    }

    [Fact]
    public async Task GetDatabaseAsync_Should_Include_Correct_Counts()
    {
        await using ApplicationTestDatabase database = await ApplicationTestDatabase.CreateAsync();
        database.Context.Documents.Add(new Document { Name = "test.pdf" });
        database.Context.IngestionJobs.Add(new IngestionJob
        {
            DocumentId = Guid.NewGuid(),
            Status = IngestionJobStatus.Failed,
            ErrorMessage = "Error"
        });
        await database.Context.SaveChangesAsync();

        LocalDiagnosticsService service = CreateService(database.Context);

        DiagnosticsDatabaseDto result = await service.GetDatabaseAsync();

        Assert.Equal(1, result.DocumentsCount);
        Assert.Equal(1, result.FailedIngestionJobsCount);
    }

    [Fact]
    public async Task GetRuntimeAsync_Should_Return_Degraded_When_Models_Missing()
    {
        await using ApplicationTestDatabase database = await ApplicationTestDatabase.CreateAsync();
        IAiRuntimeProviderRegistry registry = new StubAiRuntimeProviderRegistry(modelsAvailable: false);
        LocalDiagnosticsService service = CreateService(database.Context, aiRuntimeProviders: registry);

        DiagnosticsRuntimeDto result = await service.GetRuntimeAsync();

        Assert.Equal(DiagnosticsHealthStatus.Degraded, result.Status);
        Assert.False(result.AiRuntimeStatus.ModelsAvailable);
    }

    [Fact]
    public async Task GetStorageAsync_Should_Return_Degraded_When_Paths_Missing()
    {
        await using ApplicationTestDatabase database = await ApplicationTestDatabase.CreateAsync();
        IAppPathProvider paths = new StubPathProvider(dbExists: false);
        LocalDiagnosticsService service = CreateService(database.Context, paths: paths);

        DiagnosticsStorageDto result = await service.GetStorageAsync();

        Assert.Equal(DiagnosticsHealthStatus.Degraded, result.Status);
    }

    private static LocalDiagnosticsService CreateService(
        AppDbContext dbContext,
        IAppPathProvider? paths = null,
        IAiRuntimeProviderRegistry? aiRuntimeProviders = null)
    {
        return new LocalDiagnosticsService(
            paths ?? new StubPathProvider(),
            Options.Create(new RuntimeModeOptions { Portable = false }),
            aiRuntimeProviders ?? new StubAiRuntimeProviderRegistry(),
            dbContext);
    }

    private sealed class StubPathProvider(bool dbExists = true) : IAppPathProvider
    {
        public string AppRootDirectory => "root";
        public string DataDirectory => "data";
        public string DatabasePath => dbExists ? CreateTempFile() : "missing.db";
        public string FilesDirectory => "files";
        public string IndexDirectory => "indexes";
        public string LogsDirectory => "logs";

        private static string CreateTempFile()
        {
            string path = Path.GetTempFileName();
            return path;
        }
    }

    private sealed class StubAiRuntimeProviderRegistry(bool modelsAvailable = true) : IAiRuntimeProviderRegistry
    {
        public IReadOnlyCollection<IAiRuntimeProvider> Providers => [];
        public IAiRuntimeProvider GetSelectedProvider() => new StubAiRuntimeProvider(modelsAvailable);
    }

    private sealed class StubAiRuntimeProvider(bool modelsAvailable = true) : IAiRuntimeProvider
    {
        public string ProviderId => "stub";
        public string ProviderName => "Stub";
        public string EmbeddingModelName => "stub-model";
        public AiRuntimeProviderCapabilities Capabilities => new(true, true, true, true, true, true);

        public Task<RuntimeStatusDto> GetStatusAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(new RuntimeStatusDto(true, "Running", modelsAvailable, false));

        public Task StartAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task StopAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<IReadOnlyCollection<string>> ListModelsAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyCollection<string>>([]);
        public Task<string> GenerateChatCompletionAsync(ChatModelRequest request, CancellationToken cancellationToken = default) => Task.FromResult(string.Empty);
        public async IAsyncEnumerable<string> GenerateChatCompletionStreamAsync(ChatModelRequest request, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            yield return string.Empty;
            await Task.Yield();
        }

        public Task<float[]> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default) => Task.FromResult(Array.Empty<float>());
    }
}
