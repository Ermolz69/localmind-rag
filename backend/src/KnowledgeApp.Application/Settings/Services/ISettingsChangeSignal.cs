namespace KnowledgeApp.Application.Settings;

public interface ISettingsChangeSignal
{
    ValueTask<bool> PublishAsync(CancellationToken cancellationToken = default);

    ValueTask ReadAsync(CancellationToken cancellationToken = default);
}
