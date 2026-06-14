using KnowledgeApp.Infrastructure.Settings;

namespace KnowledgeApp.UnitTests.Settings;

public sealed class SettingsChangeSignalTests
{
    [Fact]
    public async Task PublishAsync_Should_Wake_A_Waiting_Reader()
    {
        SettingsChangeSignal signal = new();
        ValueTask read = signal.ReadAsync();

        await signal.PublishAsync();
        await read.AsTask().WaitAsync(TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task PublishAsync_Should_Coalesce_Updates_While_A_Signal_Is_Pending()
    {
        SettingsChangeSignal signal = new();

        await signal.PublishAsync();
        await signal.PublishAsync();
        await signal.ReadAsync();

        using CancellationTokenSource cancellation = new(TimeSpan.FromMilliseconds(50));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => signal.ReadAsync(cancellation.Token).AsTask());
    }

    [Fact]
    public async Task ReadAsync_Should_Respect_Cancellation()
    {
        SettingsChangeSignal signal = new();
        using CancellationTokenSource cancellation = new();
        await cancellation.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => signal.ReadAsync(cancellation.Token).AsTask());
    }
}
