namespace KnowledgeApp.Contracts.Settings;

/// <summary>Diagnostics settings.</summary>
/// <param name="Enabled">Whether diagnostics panel and page are enabled.</param>
/// <param name="DeveloperModeEnabled">Whether advanced local development settings are visible.</param>
/// <param name="MinimumLogLevel">Minimum application log level.</param>
/// <param name="UseSeparateLogFiles">Whether diagnostics are written to category-specific files.</param>
/// <param name="EnableErrorLogs">Whether warnings and errors are written to the error log file.</param>
/// <param name="EnableSqlLogs">Whether database command logs are written.</param>
/// <param name="EnableHttpLogs">Whether HTTP request logs are written.</param>
/// <param name="EnableDiagnosticEventLogs">Whether diagnostic events are written to a structured log file.</param>
/// <param name="EnableDebugTrace">Whether debug trace logs are written.</param>
public sealed record DiagnosticsSettingsDto(
    bool Enabled,
    bool DeveloperModeEnabled = false,
    string MinimumLogLevel = "Information",
    bool UseSeparateLogFiles = false,
    bool EnableErrorLogs = true,
    bool EnableSqlLogs = false,
    bool EnableHttpLogs = true,
    bool EnableDiagnosticEventLogs = false,
    bool EnableDebugTrace = false);
