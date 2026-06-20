using KnowledgeApp.Application.Abstractions;
using KnowledgeApp.Application.Settings;
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
        AddRuntimeLogSettings(builder.Services, builder.Configuration, builder.Environment);

        builder.Services.AddSingleton<ILogMaintenanceService, LogMaintenanceService>();
        builder.Services.AddHostedService<LogRetentionService>();

        builder.Host.UseSerilog((context, services, logger) =>
        {
            ConfigurationReaderOptions readerOptions = new ConfigurationReaderOptions(typeof(ConsoleLoggerConfigurationExtensions).Assembly);
            logger.ReadFrom.Configuration(context.Configuration, readerOptions);
            ConfigureDefaultSinks(
                logger,
                ResolveOptions(context.Configuration, context.HostingEnvironment),
                services.GetRequiredService<RuntimeLogLevelSwitches>());
        });

        return builder;
    }

    public static HostApplicationBuilder AddKnowledgeAppObservability(this HostApplicationBuilder builder)
    {
        AddDiagnosticLogger(builder.Services, builder.Configuration);
        AddRuntimeLogSettings(builder.Services, builder.Configuration, builder.Environment);
        builder.Services.AddSerilog((provider, logger) =>
            ConfigureDefaultSinks(
                logger,
                ResolveOptions(builder.Configuration, builder.Environment),
                provider.GetRequiredService<RuntimeLogLevelSwitches>()));
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

    private static void AddRuntimeLogSettings(
        IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        ObservabilityOptions options = ResolveOptions(configuration, environment);
        RuntimeLogLevelSwitches switches = new();
        switches.Apply(options);

        services.AddSingleton(switches);
        services.AddSingleton<ILogSettingsApplier, SerilogLogSettingsApplier>();
    }

    private static void ConfigureDefaultSinks(
        LoggerConfiguration logger,
        ObservabilityOptions options,
        RuntimeLogLevelSwitches switches)
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
        string httpLogPath = Path.Combine(options.LogsPath, "http.log");
        string sqlLogPath = Path.Combine(options.LogsPath, "sql.log");
        const string textOutputTemplate = "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}";

        logger
            .MinimumLevel.ControlledBy(switches.Application)
            .MinimumLevel.Override("Microsoft.AspNetCore", switches.Http)
            .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Database.Command", switches.Sql)
            .Enrich.FromLogContext()
            .WriteTo.Console()
            .WriteTo.File(
                appLogPath,
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: options.RetainedFileCountLimit,
                shared: true,
                outputTemplate: textOutputTemplate);

        logger.WriteTo.Logger(configuration => configuration
            .Filter.ByIncludingOnly(logEvent =>
                switches.ErrorLogsEnabled && logEvent.Level >= LogEventLevel.Warning)
            .WriteTo.File(
                errorLogPath,
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: options.RetainedFileCountLimit,
                shared: true,
                outputTemplate: textOutputTemplate));

        logger.WriteTo.Logger(configuration => configuration
            .Filter.ByIncludingOnly(logEvent =>
                switches.HttpLogsEnabled &&
                logEvent.Properties.TryGetValue("EventKind", out LogEventPropertyValue? value) &&
                value.ToString().Contains("HttpRequest", StringComparison.Ordinal))
            .WriteTo.File(
                httpLogPath,
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: options.RetainedFileCountLimit,
                shared: true,
                outputTemplate: textOutputTemplate));

        logger.WriteTo.Logger(configuration => configuration
            .Filter.ByIncludingOnly(logEvent =>
                switches.SqlLogsEnabled &&
                logEvent.MessageTemplate.Text.StartsWith("SQL ", StringComparison.Ordinal))
            .WriteTo.File(
                sqlLogPath,
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: options.RetainedFileCountLimit,
                shared: true,
                outputTemplate: textOutputTemplate));

        if (options.Mode == ObservabilityMode.Advanced)
        {
            logger.WriteTo.Logger(configuration => configuration
                .Filter.ByIncludingOnly(logEvent =>
                    switches.DiagnosticEventLogsEnabled &&
                    logEvent.Properties.TryGetValue("EventKind", out LogEventPropertyValue? value) &&
                    value.ToString().Contains("Diagnostic", StringComparison.Ordinal))
                .WriteTo.File(
                    new CompactJsonFormatter(),
                    advancedLogPath,
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: options.RetainedFileCountLimit,
                    shared: true));
        }

        logger.WriteTo.Logger(configuration => configuration
            .Filter.ByIncludingOnly(_ => switches.DebugTraceEnabled)
            .MinimumLevel.Debug()
            .WriteTo.File(
                new CompactJsonFormatter(),
                debugTracePath,
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: options.RetainedFileCountLimit,
                shared: true));
    }
}
