using KnowledgeApp.Application.Abstractions.Ingestion;
using KnowledgeApp.Infrastructure.Options;
using KnowledgeApp.Infrastructure.Services.Runtime;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace KnowledgeApp.Infrastructure.Services;

public sealed class QueuedIngestionHostedService(
    IServiceProvider services,
    IOptions<IngestionWorkerOptions> options,
    IIngestionJobSignal signal,
    RuntimeProcessManager processManager,
    ILogger<QueuedIngestionHostedService> logger) : BackgroundService
{
    private readonly IngestionWorkerOptions options = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.Enabled)
        {
            logger.LogInformation("Queued ingestion worker is disabled.");
            return;
        }

        TimeSpan recoveryInterval = TimeSpan.FromSeconds(
            Math.Max(1, options.RecoveryIntervalSeconds));
        logger.LogInformation(
            "Queued ingestion worker started in event-driven mode with recovery interval {RecoveryInterval}.",
            recoveryInterval);

        await RecoverPendingJobsAsync(stoppingToken);

        using PeriodicTimer timer = new(recoveryInterval);
        Task<Guid> readTask = signal.ReadAsync(stoppingToken).AsTask();
        Task<bool> recoveryTask = timer.WaitForNextTickAsync(stoppingToken).AsTask();

        try
        {
            while (!stoppingToken.IsCancellationRequested && !processManager.IsShuttingDown)
            {
                Task completedTask = await Task.WhenAny(readTask, recoveryTask);

                if (completedTask == readTask)
                {
                    Guid jobId = await readTask;
                    await ProcessSignaledJobAsync(jobId, stoppingToken);
                    readTask = signal.ReadAsync(stoppingToken).AsTask();
                }

                if (completedTask == recoveryTask)
                {
                    if (!await recoveryTask)
                    {
                        break;
                    }

                    await RecoverPendingJobsAsync(stoppingToken);
                    recoveryTask = timer.WaitForNextTickAsync(stoppingToken).AsTask();
                }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
    }

    private async Task ProcessSignaledJobAsync(
        Guid jobId,
        CancellationToken cancellationToken)
    {
        try
        {
            using IServiceScope scope = services.CreateScope();
            QueuedIngestionJobDispatcher dispatcher =
                scope.ServiceProvider.GetRequiredService<QueuedIngestionJobDispatcher>();
            await dispatcher.ProcessJobAsync(jobId, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Queued ingestion worker failed to dispatch job {JobId}.", jobId);
        }
        finally
        {
            signal.Complete(jobId);
        }
    }

    private async Task RecoverPendingJobsAsync(CancellationToken cancellationToken)
    {
        try
        {
            using IServiceScope scope = services.CreateScope();
            QueuedIngestionJobDispatcher dispatcher =
                scope.ServiceProvider.GetRequiredService<QueuedIngestionJobDispatcher>();
            int recovered = await dispatcher.RecoverPendingBatchAsync(cancellationToken);

            if (recovered > 0)
            {
                logger.LogInformation(
                    "Recovered {RecoveredJobCount} pending ingestion jobs.",
                    recovered);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Queued ingestion recovery failed.");
        }
    }
}
