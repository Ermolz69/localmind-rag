using KnowledgeApp.Application.Abstractions;
using KnowledgeApp.Application.Settings;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace KnowledgeApp.Observability;

/// <summary>
/// Periodically removes log files older than the configured retention window
/// (Diagnostics.LogRetainedDays), reusing the daily-rolling files Serilog produces.
/// </summary>
public sealed class LogRetentionService(
    IServiceScopeFactory scopeFactory,
    ILogMaintenanceService logMaintenance,
    ILogger<LogRetentionService> logger) : BackgroundService
{
    private const int FallbackRetainedDays = 14;
    private static readonly TimeSpan InitialDelay = TimeSpan.FromMinutes(1);
    private static readonly TimeSpan SweepInterval = TimeSpan.FromHours(24);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await Task.Delay(InitialDelay, stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                await SweepAsync(stoppingToken);
                await Task.Delay(SweepInterval, stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            // Host is shutting down.
        }
    }

    private async Task SweepAsync(CancellationToken cancellationToken)
    {
        try
        {
            int retainedDays = await ResolveRetainedDaysAsync(cancellationToken);
            await logMaintenance.RemoveOlderThanAsync(retainedDays, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Scheduled log retention sweep failed.");
        }
    }

    private async Task<int> ResolveRetainedDaysAsync(CancellationToken cancellationToken)
    {
        using IServiceScope scope = scopeFactory.CreateScope();
        ISettingsService settings = scope.ServiceProvider.GetRequiredService<ISettingsService>();
        var current = await settings.GetAsync(cancellationToken);
        int retainedDays = current.Diagnostics?.LogRetainedDays ?? FallbackRetainedDays;

        return retainedDays > 0 ? retainedDays : FallbackRetainedDays;
    }
}
