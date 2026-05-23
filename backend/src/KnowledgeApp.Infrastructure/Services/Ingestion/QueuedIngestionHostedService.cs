using KnowledgeApp.Infrastructure.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace KnowledgeApp.Infrastructure.Services;

public sealed class QueuedIngestionHostedService(
    IServiceProvider services,
    IOptions<IngestionWorkerOptions> options,
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

        TimeSpan pollInterval = TimeSpan.FromSeconds(Math.Max(1, options.PollIntervalSeconds));
        logger.LogInformation("Queued ingestion worker started with poll interval {PollInterval}.", pollInterval);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using IServiceScope scope = services.CreateScope();
                QueuedIngestionJobDispatcher dispatcher = scope.ServiceProvider.GetRequiredService<QueuedIngestionJobDispatcher>();
                await dispatcher.ProcessNextBatchAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Queued ingestion worker loop failed.");
            }

            await Task.Delay(pollInterval, stoppingToken);
        }
    }
}
