using Serilog.Core;
using Serilog.Events;

namespace KnowledgeApp.Observability;

public sealed class RuntimeLogLevelSwitches
{
    public LoggingLevelSwitch Application { get; } = new(LogEventLevel.Information);

    public LoggingLevelSwitch Http { get; } = new(LogEventLevel.Information);

    public LoggingLevelSwitch Sql { get; } = new(LogEventLevel.Warning);

    public bool UseSeparateLogFiles { get; private set; }

    public bool ErrorLogsEnabled { get; private set; }

    public bool HttpLogsEnabled { get; private set; }

    public bool SqlLogsEnabled { get; private set; }

    public bool DiagnosticEventLogsEnabled { get; private set; }

    public bool DebugTraceEnabled { get; private set; }

    public void Apply(ObservabilityOptions options)
    {
        Application.MinimumLevel = ToSerilogLevel(options.MinimumLevel);
        Http.MinimumLevel = options.EnableHttpLogs ? LogEventLevel.Information : LogEventLevel.Warning;
        Sql.MinimumLevel = options.EnableSqlLogs ? LogEventLevel.Information : LogEventLevel.Warning;
        UseSeparateLogFiles = options.UseSeparateLogFiles;
        ErrorLogsEnabled = options.UseSeparateLogFiles && options.EnableErrorLogs;
        HttpLogsEnabled = options.UseSeparateLogFiles && options.EnableHttpLogs;
        SqlLogsEnabled = options.UseSeparateLogFiles && options.EnableSqlLogs;
        DiagnosticEventLogsEnabled = options.UseSeparateLogFiles && options.EnableDiagnosticEventLogs;
        DebugTraceEnabled = options.UseSeparateLogFiles && options.EnableDebugTrace;
    }

    private static LogEventLevel ToSerilogLevel(Microsoft.Extensions.Logging.LogLevel level)
    {
        return level switch
        {
            Microsoft.Extensions.Logging.LogLevel.Trace => LogEventLevel.Verbose,
            Microsoft.Extensions.Logging.LogLevel.Debug => LogEventLevel.Debug,
            Microsoft.Extensions.Logging.LogLevel.Information => LogEventLevel.Information,
            Microsoft.Extensions.Logging.LogLevel.Warning => LogEventLevel.Warning,
            Microsoft.Extensions.Logging.LogLevel.Error => LogEventLevel.Error,
            Microsoft.Extensions.Logging.LogLevel.Critical => LogEventLevel.Fatal,
            Microsoft.Extensions.Logging.LogLevel.None => LogEventLevel.Fatal,
            _ => LogEventLevel.Information,
        };
    }
}
