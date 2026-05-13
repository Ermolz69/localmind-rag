using KnowledgeApp.Application;
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
        builder.Services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy => policy
                .AllowAnyHeader()
                .AllowAnyMethod()
                .SetIsOriginAllowed(origin => origin.StartsWith("http://127.0.0.1:", StringComparison.OrdinalIgnoreCase)
                    || origin.StartsWith("http://localhost:", StringComparison.OrdinalIgnoreCase)));
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
}
