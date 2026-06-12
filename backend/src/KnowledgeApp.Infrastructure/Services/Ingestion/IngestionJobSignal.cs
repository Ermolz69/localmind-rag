using System.Collections.Concurrent;
using System.Threading.Channels;
using KnowledgeApp.Application.Abstractions.Ingestion;

namespace KnowledgeApp.Infrastructure.Services;

public sealed class IngestionJobSignal : IIngestionJobSignal
{
    private readonly ConcurrentDictionary<Guid, byte> pendingJobIds = new();
    private readonly Channel<Guid> channel = Channel.CreateUnbounded<Guid>(
        new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false,
            AllowSynchronousContinuations = false,
        });

    public async ValueTask<bool> PublishAsync(
        Guid jobId,
        CancellationToken cancellationToken = default)
    {
        if (!pendingJobIds.TryAdd(jobId, 0))
        {
            return false;
        }

        try
        {
            await channel.Writer.WriteAsync(jobId, cancellationToken);
            return true;
        }
        catch
        {
            pendingJobIds.TryRemove(jobId, out _);
            throw;
        }
    }

    public ValueTask<Guid> ReadAsync(CancellationToken cancellationToken = default)
    {
        return channel.Reader.ReadAsync(cancellationToken);
    }

    public void Complete(Guid jobId)
    {
        pendingJobIds.TryRemove(jobId, out _);
    }
}
