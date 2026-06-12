using KnowledgeApp.Application.Abstractions;
using KnowledgeApp.Application.Abstractions.Ingestion;
using KnowledgeApp.Domain.Entities;
using KnowledgeApp.Domain.Enums;
using KnowledgeApp.Infrastructure.Options;
using KnowledgeApp.Infrastructure.Services;
using KnowledgeApp.Infrastructure.Services.Runtime;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace KnowledgeApp.UnitTests.Ingestion;

public sealed class QueuedIngestionHostedServiceTests
{
    [Fact]
    public async Task StartAsync_Should_Recover_Pending_Jobs()
    {
        await using ApplicationTestDatabase database =
            await ApplicationTestDatabase.CreateAsync();
        IngestionJob job = new()
        {
            DocumentId = Guid.NewGuid(),
            Status = IngestionJobStatus.Pending,
        };
        database.Context.IngestionJobs.Add(job);
        await database.Context.SaveChangesAsync();
        RecordingProcessor processor = new();
        IngestionJobSignal signal = new();
        await using ServiceProvider services = CreateServices(database, processor);
        QueuedIngestionHostedService hostedService = CreateHostedService(
            services,
            signal,
            recoveryIntervalSeconds: 60);

        await hostedService.StartAsync(CancellationToken.None);

        Assert.Equal(job.Id, await processor.WaitForFirstJobAsync());
        await hostedService.StopAsync(CancellationToken.None);
    }

    [Fact]
    public async Task Published_Job_Should_Wake_Worker_Immediately()
    {
        await using ApplicationTestDatabase database =
            await ApplicationTestDatabase.CreateAsync();
        RecordingProcessor processor = new();
        IngestionJobSignal signal = new();
        await using ServiceProvider services = CreateServices(database, processor);
        QueuedIngestionHostedService hostedService = CreateHostedService(
            services,
            signal,
            recoveryIntervalSeconds: 60);
        Guid jobId = Guid.NewGuid();

        await hostedService.StartAsync(CancellationToken.None);
        await signal.PublishAsync(jobId);

        Assert.Equal(jobId, await processor.WaitForFirstJobAsync());
        await hostedService.StopAsync(CancellationToken.None);
    }

    [Fact]
    public async Task Recovery_Timer_Should_Process_Job_Without_Signal()
    {
        await using ApplicationTestDatabase database =
            await ApplicationTestDatabase.CreateAsync();
        RecordingProcessor processor = new();
        IngestionJobSignal signal = new();
        await using ServiceProvider services = CreateServices(database, processor);
        QueuedIngestionHostedService hostedService = CreateHostedService(
            services,
            signal,
            recoveryIntervalSeconds: 1);

        await hostedService.StartAsync(CancellationToken.None);
        await Task.Delay(100);

        IngestionJob job = new()
        {
            DocumentId = Guid.NewGuid(),
            Status = IngestionJobStatus.Pending,
        };
        database.Context.IngestionJobs.Add(job);
        await database.Context.SaveChangesAsync();

        Assert.Equal(job.Id, await processor.WaitForFirstJobAsync());
        await hostedService.StopAsync(CancellationToken.None);
    }

    [Fact]
    public async Task StopAsync_Should_Not_Wait_For_Recovery_Timer()
    {
        await using ApplicationTestDatabase database =
            await ApplicationTestDatabase.CreateAsync();
        RecordingProcessor processor = new();
        IngestionJobSignal signal = new();
        await using ServiceProvider services = CreateServices(database, processor);
        QueuedIngestionHostedService hostedService = CreateHostedService(
            services,
            signal,
            recoveryIntervalSeconds: 60);

        await hostedService.StartAsync(CancellationToken.None);
        Task stopTask = hostedService.StopAsync(CancellationToken.None);

        Assert.Same(stopTask, await Task.WhenAny(stopTask, Task.Delay(TimeSpan.FromSeconds(5))));
    }

    private static ServiceProvider CreateServices(
        ApplicationTestDatabase database,
        RecordingProcessor processor)
    {
        ServiceCollection services = new();
        services.AddSingleton<IIngestionJobRepository>(
            new IngestionJobRepository(database.Context));
        services.AddSingleton<IIngestionJobProcessor>(processor);
        services.AddSingleton<IOptions<IngestionWorkerOptions>>(
            Options.Create(new IngestionWorkerOptions { RecoveryBatchSize = 100 }));
        services.AddScoped<QueuedIngestionJobDispatcher>();
        services.AddSingleton<ILogger<QueuedIngestionJobDispatcher>>(
            NullLogger<QueuedIngestionJobDispatcher>.Instance);
        return services.BuildServiceProvider();
    }

    private static QueuedIngestionHostedService CreateHostedService(
        IServiceProvider services,
        IIngestionJobSignal signal,
        int recoveryIntervalSeconds)
    {
        RuntimeProcessManager processManager = new(
            new FakeApplicationLifetime(),
            NullLogger<RuntimeProcessManager>.Instance);

        return new QueuedIngestionHostedService(
            services,
            Options.Create(
                new IngestionWorkerOptions
                {
                    Enabled = true,
                    RecoveryIntervalSeconds = recoveryIntervalSeconds,
                    RecoveryBatchSize = 100,
                }),
            signal,
            processManager,
            NullLogger<QueuedIngestionHostedService>.Instance);
    }

    private sealed class RecordingProcessor : IIngestionJobProcessor
    {
        private readonly TaskCompletionSource<Guid> firstJob =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task ProcessAsync(
            Guid jobId,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            firstJob.TrySetResult(jobId);
            return Task.CompletedTask;
        }

        public async Task<Guid> WaitForFirstJobAsync()
        {
            Task completed = await Task.WhenAny(
                firstJob.Task,
                Task.Delay(TimeSpan.FromSeconds(10)));
            Assert.Same(firstJob.Task, completed);
            return await firstJob.Task;
        }
    }
}
