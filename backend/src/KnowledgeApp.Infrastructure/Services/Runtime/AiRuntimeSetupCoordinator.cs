using System.Diagnostics;
using System.Threading.Channels;

using KnowledgeApp.Application.Abstractions;
using KnowledgeApp.Contracts.Runtime;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace KnowledgeApp.Infrastructure.Services;

public sealed class AiRuntimeSetupCoordinator(
    IServiceProvider serviceProvider,
    ILogger<AiRuntimeSetupCoordinator> logger) : IAiRuntimeSetupCoordinator
{
    private readonly object lockObj = new();
    private Guid? activeSetupId;
    private Channel<RuntimeSetupProgress>? activeChannel;

    public RuntimeSetupStartedResponse StartSetup(CancellationToken cancellationToken = default)
    {
        lock (lockObj)
        {
            if (activeSetupId is not null)
            {
                return new RuntimeSetupStartedResponse(activeSetupId.Value, AlreadyRunning: true);
            }

            activeSetupId = Guid.NewGuid();
            activeChannel = Channel.CreateUnbounded<RuntimeSetupProgress>();

            Guid setupId = activeSetupId.Value;
            Channel<RuntimeSetupProgress> channel = activeChannel;

            // Start background execution
            _ = Task.Run(async () => await RunSetupAsync(setupId, channel, CancellationToken.None));

            return new RuntimeSetupStartedResponse(setupId, AlreadyRunning: false);
        }
    }

    public async IAsyncEnumerable<RuntimeSetupProgress> WatchProgressAsync(
        Guid setupId,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        Channel<RuntimeSetupProgress>? channel;

        lock (lockObj)
        {
            if (activeSetupId != setupId || activeChannel is null)
            {
                yield break;
            }

            channel = activeChannel;
        }

        await foreach (RuntimeSetupProgress progress in channel.Reader.ReadAllAsync(cancellationToken))
        {
            yield return progress;
        }
    }

    private async Task RunSetupAsync(
        Guid setupId,
        Channel<RuntimeSetupProgress> channel,
        CancellationToken cancellationToken)
    {
        try
        {
            // Report starting
            await channel.Writer.WriteAsync(new RuntimeSetupProgress(
                Stage: "checking",
                Message: "Starting setup..."), cancellationToken);

            using IServiceScope scope = serviceProvider.CreateScope();
            IAiRuntimeSetupService setupService = scope.ServiceProvider.GetRequiredService<IAiRuntimeSetupService>();

            Progress<RuntimeSetupProgress> progressReporter = new(progress =>
            {
                // Fire and forget writing to unbounded channel
                channel.Writer.TryWrite(progress);
            });

            await setupService.SetupAsync(progressReporter, cancellationToken);

            await channel.Writer.WriteAsync(new RuntimeSetupProgress(
                Stage: "completed",
                Message: "AI runtime installed successfully.",
                IsCompleted: true), cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "AI runtime setup failed.");

            await channel.Writer.WriteAsync(new RuntimeSetupProgress(
                Stage: "failed",
                Message: $"Setup failed: {ex.Message}",
                IsFailed: true), cancellationToken);
        }
        finally
        {
            channel.Writer.TryComplete();

            lock (lockObj)
            {
                if (activeSetupId == setupId)
                {
                    activeSetupId = null;
                    activeChannel = null;
                }
            }
        }
    }
}
