namespace LocalMind.Sync.Worker.Consumers;

using LocalMind.Sync.Contracts.Queues;
using MassTransit;

public sealed class PushRequestedConsumer : IConsumer<SyncPushRequestedMessage>
{
    private readonly ILogger<PushRequestedConsumer> logger;

    public PushRequestedConsumer(ILogger<PushRequestedConsumer> logger)
    {
        this.logger = logger;
    }

    public Task Consume(ConsumeContext<SyncPushRequestedMessage> context)
    {
        SyncPushRequestedMessage message = context.Message;
        logger.LogInformation(
            "Received sync push request {MessageId} for device {DeviceId} with {ChangeCount} changes",
            message.MessageId,
            message.DeviceId,
            message.ChangeCount);

        return Task.CompletedTask;
    }
}
