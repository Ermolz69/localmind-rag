using KnowledgeApp.Application.Abstractions;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Compact;
using Serilog.Settings.Configuration;

namespace KnowledgeApp.Observability;

public static class ObservabilityDependencyInjection
{
    public static WebApplicationBuilder AddKnowledgeAppObservability(this WebApplicationBuilder builder)
    {
        AddDiagnosticLogger(builder.Services, builder.Configuration);

        builder.Host.UseSerilog((context, logger) =>
        {
            ConfigurationReaderOptions readerOptions = new ConfigurationReaderOptions(typeof(ConsoleLoggerConfigurationExtensions).Assembly);
            logger.ReadFrom.Configuration(context.Configuration, readerOptions);
            ConfigureDefaultSinks(logger, ResolveOptions(context.Configuration, context.HostingEnvironment));
        });

        return builder;
    }

    public static HostApplicationBuilder AddKnowledgeAppObservability(this HostApplicationBuilder builder)
    {
        AddDiagnosticLogger(builder.Services, builder.Configuration);
        builder.Services.AddSerilog(logger => ConfigureDefaultSinks(logger, ResolveOptions(builder.Configuration, builder.Environment)));
        return builder;
    }

    public static WebApplication UseKnowledgeAppObservability(this WebApplication app)
    {
        app.UseMiddleware<RequestLoggingMiddleware>();
        return app;
    }

    private static ObservabilityOptions ResolveOptions(IConfiguration configuration, IHostEnvironment environment)
    {
        ObservabilityOptions options = new();

        configuration.GetSection("Observability").Bind(options);

        if (environment.IsDevelopment()
            && string.IsNullOrWhiteSpace(configuration["Observability:EnableDebugTrace"]))
        {
            options.EnableDebugTrace = true;
        }

        return options;
    }

    private static void AddDiagnosticLogger(IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<ObservabilityOptions>(configuration.GetSection("Observability"));
        services.AddSingleton<IAppDiagnosticLogger>(provider =>
            provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<ObservabilityOptions>>().Value.Enabled
                ? new AppDiagnosticLogger(
                    provider.GetRequiredService<ILogger<AppDiagnosticLogger>>(),
                    provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<ObservabilityOptions>>())
                : new NoopAppDiagnosticLogger());
    }

    private static void ConfigureDefaultSinks(LoggerConfiguration logger, ObservabilityOptions options)
    {
        if (!options.Enabled)
        {
            logger.MinimumLevel.Fatal();
            return;
        }

        Directory.CreateDirectory(options.LogsPath);

        string appLogPath = Path.Combine(options.LogsPath, "localmind.log");
        string errorLogPath = Path.Combine(options.LogsPath, "errors.log");
        string advancedLogPath = Path.Combine(options.LogsPath, "advanced-events.ndjson");
        string debugTracePath = Path.Combine(options.LogsPath, "debug-trace.ndjson");
        const string textOutputTemplate = "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}";

        logger
            .MinimumLevel.Is(ToSerilogLevel(options.MinimumLevel))
            .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .WriteTo.Console()
            .WriteTo.File(
                appLogPath,
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: options.RetainedFileCountLimit,
                shared: true,
                outputTemplate: textOutputTemplate)
            .WriteTo.File(
                errorLogPath,
                restrictedToMinimumLevel: LogEventLevel.Warning,
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: options.RetainedFileCountLimit,
                shared: true,
                outputTemplate: textOutputTemplate);

        if (options.Mode == ObservabilityMode.Advanced)
        {
            logger.WriteTo.Logger(configuration => configuration
                .Filter.ByIncludingOnly(logEvent =>
                    logEvent.Properties.TryGetValue("EventKind", out LogEventPropertyValue? value) &&
                    value.ToString().Contains("Diagnostic", StringComparison.Ordinal))
                .WriteTo.File(
                    new CompactJsonFormatter(),
                    advancedLogPath,
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: options.RetainedFileCountLimit,
                    shared: true));
        }

        if (options.EnableDebugTrace)
        {
            logger.WriteTo.File(
                new CompactJsonFormatter(),
                debugTracePath,
                restrictedToMinimumLevel: LogEventLevel.Debug,
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: options.RetainedFileCountLimit,
                shared: true);
        }
    }

    private static LogEventLevel ToSerilogLevel(LogLevel level)
    {
        return level switch
        {
            LogLevel.Trace => LogEventLevel.Verbose,
            LogLevel.Debug => LogEventLevel.Debug,
            LogLevel.Information => LogEventLevel.Information,
            LogLevel.Warning => LogEventLevel.Warning,
            LogLevel.Error => LogEventLevel.Error,
            LogLevel.Critical => LogEventLevel.Fatal,
            _ => LogEventLevel.Information,
        };
    }
}
