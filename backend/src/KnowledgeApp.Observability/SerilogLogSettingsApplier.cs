using KnowledgeApp.Application.Settings;
using KnowledgeApp.Contracts.Settings;
using Microsoft.Extensions.Logging;

namespace KnowledgeApp.Observability;

public sealed class SerilogLogSettingsApplier(RuntimeLogLevelSwitches switches) : ILogSettingsApplier
{
    public void Apply(DiagnosticsSettingsDto settings)
    {
        switches.Apply(new ObservabilityOptions
        {
            MinimumLevel = ToLogLevel(settings.MinimumLogLevel),
            UseSeparateLogFiles = settings.UseSeparateLogFiles,
            EnableErrorLogs = settings.EnableErrorLogs,
            EnableSqlLogs = settings.EnableSqlLogs,
            EnableHttpLogs = settings.EnableHttpLogs,
            EnableDiagnosticEventLogs = settings.EnableDiagnosticEventLogs,
            EnableDebugTrace = settings.EnableDebugTrace,
        });
    }

    private static LogLevel ToLogLevel(string value)
    {
        return Enum.TryParse(value, ignoreCase: true, out LogLevel level)
            ? level
            : LogLevel.Information;
    }
}
