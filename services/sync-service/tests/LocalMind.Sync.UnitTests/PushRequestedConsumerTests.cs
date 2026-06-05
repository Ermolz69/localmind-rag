namespace LocalMind.Sync.UnitTests;

using LocalMind.Sync.Contracts.Queues;
using LocalMind.Sync.Worker.Consumers;
using MassTransit;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

public sealed class PushRequestedConsumerTests
{
    [Fact]
    public async Task ConsumerProcessesPushRequestedMessage()
    {
        SyncPushRequestedMessage message = new(Guid.NewGuid(), Guid.NewGuid(), 3, DateTimeOffset.UtcNow);
        ConsumeContext<SyncPushRequestedMessage> context = Substitute.For<ConsumeContext<SyncPushRequestedMessage>>();
        context.Message.Returns(message);
        PushRequestedConsumer consumer = new(NullLogger<PushRequestedConsumer>.Instance);

        await consumer.Consume(context);

        Assert.Equal(message, context.Message);
    }
}
