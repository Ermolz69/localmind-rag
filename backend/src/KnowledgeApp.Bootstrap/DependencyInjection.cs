using KnowledgeApp.Application;
using KnowledgeApp.Bootstrap.ProblemDetails;
using KnowledgeApp.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Settings.Configuration;

namespace KnowledgeApp.Bootstrap;

public static class DependencyInjection
{
    public static WebApplicationBuilder AddKnowledgeAppBootstrap(this WebApplicationBuilder builder)
    {
        builder.Host.UseSerilog((context, logger) =>
        {
            var readerOptions = new ConfigurationReaderOptions(typeof(ConsoleLoggerConfigurationExtensions).Assembly);
            logger.ReadFrom.Configuration(context.Configuration, readerOptions);
        });
        builder.Services.AddProblemDetails();
        builder.Services.AddExceptionHandler<AppExceptionHandler>();
        builder.Services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy => policy
                .AllowAnyHeader()
                .AllowAnyMethod()
                .SetIsOriginAllowed(IsAllowedDesktopOrigin));
        });
        builder.Services.AddApplication();
        builder.Services.AddInfrastructure(builder.Configuration);
        return builder;
    }

    public static WebApplication UseKnowledgeAppBootstrap(this WebApplication app)
    {
        app.UseExceptionHandler();
        app.UseStatusCodePages();
        app.UseCors();
        return app;
    }

    private static bool IsAllowedDesktopOrigin(string origin)
    {
        if (!Uri.TryCreate(origin, UriKind.Absolute, out var uri))
        {
            return false;
        }

        return uri.Scheme is "http" or "https"
            && (uri.Host.Equals("127.0.0.1", StringComparison.OrdinalIgnoreCase)
                || uri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase)
                || uri.Host.Equals("tauri.localhost", StringComparison.OrdinalIgnoreCase));
    }
}
