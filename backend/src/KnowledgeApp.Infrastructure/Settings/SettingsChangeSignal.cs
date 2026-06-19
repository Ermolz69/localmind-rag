using System.Threading.Channels;
using KnowledgeApp.Application.Settings;

namespace KnowledgeApp.Infrastructure.Settings;

public sealed class SettingsChangeSignal : ISettingsChangeSignal
{
    private readonly Channel<bool> channel = Channel.CreateBounded<bool>(
        new BoundedChannelOptions(1)
        {
            SingleReader = true,
            SingleWriter = false,
            FullMode = BoundedChannelFullMode.DropWrite,
            AllowSynchronousContinuations = false,
        });

    public ValueTask<bool> PublishAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult(channel.Writer.TryWrite(true));
    }

    public async ValueTask ReadAsync(CancellationToken cancellationToken = default)
    {
        await channel.Reader.ReadAsync(cancellationToken);
    }
}
