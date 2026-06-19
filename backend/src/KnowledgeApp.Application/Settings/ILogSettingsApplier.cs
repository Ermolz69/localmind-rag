using KnowledgeApp.Contracts.Settings;

namespace KnowledgeApp.Application.Settings;

public interface ILogSettingsApplier
{
    void Apply(DiagnosticsSettingsDto settings);
}

internal sealed class NoopLogSettingsApplier : ILogSettingsApplier
{
    public void Apply(DiagnosticsSettingsDto settings)
    {
    }
}
