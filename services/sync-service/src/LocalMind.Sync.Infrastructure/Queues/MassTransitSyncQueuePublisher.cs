namespace LocalMind.Sync.Infrastructure.Queues;

using LocalMind.Sync.Application.Abstractions;
using LocalMind.Sync.Contracts.Queues;
using MassTransit;

public sealed class MassTransitSyncQueuePublisher : ISyncQueuePublisher
{
    private readonly IClock clock;
    private readonly ISendEndpointProvider sendEndpointProvider;

    public MassTransitSyncQueuePublisher(ISendEndpointProvider sendEndpointProvider, IClock clock)
    {
        this.sendEndpointProvider = sendEndpointProvider;
        this.clock = clock;
    }

    public async Task<Guid> PublishPushRequestedAsync(Guid deviceId, int changeCount, CancellationToken cancellationToken)
    {
        Guid messageId = Guid.NewGuid();
        SyncPushRequestedMessage message = new(messageId, deviceId, changeCount, clock.UtcNow);
        await SendAsync("sync.push.requested", message, cancellationToken);
        return messageId;
    }

    public async Task<Guid> PublishPullRequestedAsync(Guid deviceId, int limit, CancellationToken cancellationToken)
    {
        Guid messageId = Guid.NewGuid();
        SyncPullRequestedMessage message = new(messageId, deviceId, limit, clock.UtcNow);
        await SendAsync("sync.pull.requested", message, cancellationToken);
        return messageId;
    }

    public async Task<Guid> PublishDiffRequestedAsync(Guid deviceId, CancellationToken cancellationToken)
    {
        Guid messageId = Guid.NewGuid();
        SyncDiffRequestedMessage message = new(messageId, deviceId, clock.UtcNow);
        await SendAsync("sync.diff.requested", message, cancellationToken);
        return messageId;
    }

    public async Task<Guid> PublishConflictDetectedAsync(Guid conflictId, string strategy, CancellationToken cancellationToken)
    {
        Guid messageId = Guid.NewGuid();
        SyncConflictDetectedMessage message = new(messageId, conflictId, strategy, clock.UtcNow);
        await SendAsync("sync.conflict.detected", message, cancellationToken);
        return messageId;
    }

    private async Task SendAsync<T>(string queueName, T message, CancellationToken cancellationToken)
        where T : class
    {
        ISendEndpoint endpoint = await sendEndpointProvider.GetSendEndpoint(new Uri($"queue:{queueName}"));
        await endpoint.Send(message, cancellationToken);
    }
}
