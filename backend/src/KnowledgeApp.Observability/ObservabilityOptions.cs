using Microsoft.Extensions.Logging;

namespace KnowledgeApp.Observability;

public sealed class ObservabilityOptions
{
    public bool Enabled { get; set; } = true;

    public ObservabilityMode Mode { get; set; } = ObservabilityMode.Advanced;

    public string LogsPath { get; set; } = "runtime/app/logs";

    public LogLevel MinimumLevel { get; set; } = LogLevel.Information;

    public bool UseSeparateLogFiles { get; set; }

    public bool EnableErrorLogs { get; set; } = true;

    public bool EnableDebugTrace { get; set; }

    public bool EnableSqlLogs { get; set; }

    public bool EnableHttpLogs { get; set; } = true;

    public bool EnableDiagnosticEventLogs { get; set; }

    public bool EnableRequestBodyLogging { get; set; }

    public bool EnableResponseBodyLogging { get; set; }

    public int RetainedFileCountLimit { get; set; } = 14;
}
