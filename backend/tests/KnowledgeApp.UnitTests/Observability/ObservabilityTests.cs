using KnowledgeApp.Observability;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace KnowledgeApp.UnitTests.Observability;

public sealed class ObservabilityTests
{
    [Fact]
    public void ObservabilityOptions_Should_Have_Privacy_First_Defaults()
    {
        ObservabilityOptions options = new();

        Assert.True(options.Enabled);
        Assert.Equal(ObservabilityMode.Advanced, options.Mode);
        Assert.Equal("runtime/app/logs", options.LogsPath);
        Assert.Equal(LogLevel.Information, options.MinimumLevel);
        Assert.False(options.UseSeparateLogFiles);
        Assert.True(options.EnableErrorLogs);
        Assert.False(options.EnableSqlLogs);
        Assert.True(options.EnableHttpLogs);
        Assert.False(options.EnableDiagnosticEventLogs);
        Assert.False(options.EnableRequestBodyLogging);
        Assert.False(options.EnableResponseBodyLogging);
        Assert.False(options.EnableDebugTrace);
    }

    [Fact]
    public void NoopAppDiagnosticLogger_Should_Not_Throw()
    {
        NoopAppDiagnosticLogger logger = new();
        Guid operationId = logger.BeginOperation("test", "noop");

        logger.LogStep(operationId, "step");
        logger.LogFailure(operationId, new InvalidOperationException("failure"));

        Assert.NotEqual(Guid.Empty, operationId);
    }

    [Fact]
    public void AppDiagnosticLogger_Should_Write_Operation_Step_And_Failure_Events_When_Enabled()
    {
        CapturingLogger<AppDiagnosticLogger> logger = new();
        AppDiagnosticLogger diagnostics = new(
            logger,
            Options.Create(new ObservabilityOptions { Enabled = true, Mode = ObservabilityMode.Advanced }));

        Guid operationId = diagnostics.BeginOperation("rag", "answer", new Dictionary<string, object?> { ["ConversationId"] = Guid.NewGuid() });
        diagnostics.LogStep(operationId, "context-built", new Dictionary<string, object?> { ["SourcesCount"] = 2 });
        diagnostics.LogFailure(operationId, new InvalidOperationException("broken"));

        Assert.Equal(3, logger.Entries.Count);
        Assert.All(logger.Entries, entry => Assert.Contains("Diagnostic", entry.ScopeText, StringComparison.Ordinal));
        Assert.Contains(logger.Entries, entry => entry.Level == LogLevel.Error && entry.Exception is InvalidOperationException);
    }

    [Fact]
    public void AppDiagnosticLogger_Should_Not_Write_When_Disabled()
    {
        CapturingLogger<AppDiagnosticLogger> logger = new();
        AppDiagnosticLogger diagnostics = new(
            logger,
            Options.Create(new ObservabilityOptions { Enabled = false, Mode = ObservabilityMode.Advanced }));

        Guid operationId = diagnostics.BeginOperation("rag", "answer");
        diagnostics.LogStep(operationId, "context-built");

        Assert.Empty(logger.Entries);
    }

    private sealed class CapturingLogger<T> : ILogger<T>
    {
        private readonly List<object?> scopes = [];

        public List<LogEntry> Entries { get; } = [];

        public IDisposable BeginScope<TState>(TState state)
            where TState : notnull
        {
            scopes.Add(state);
            return new Scope(() => scopes.Remove(state));
        }

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            Entries.Add(new LogEntry(logLevel, formatter(state, exception), exception, string.Join(" | ", scopes.Select(FormatScope))));
        }

        public sealed record LogEntry(LogLevel Level, string Message, Exception? Exception, string ScopeText);

        private static string FormatScope(object? scope)
        {
            if (scope is IEnumerable<KeyValuePair<string, object?>> values)
            {
                return string.Join(", ", values.Select(value => $"{value.Key}={value.Value}"));
            }

            return scope?.ToString() ?? string.Empty;
        }

        private sealed class Scope(Action dispose) : IDisposable
        {
            public void Dispose()
            {
                dispose();
            }
        }
    }
}
