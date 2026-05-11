using KnowledgeApp.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace KnowledgeApp.Bootstrap;

public static class DependencyInjection
{
    public static WebApplicationBuilder AddKnowledgeAppBootstrap(this WebApplicationBuilder builder)
    {
        builder.Host.UseSerilog((context, logger) => logger.ReadFrom.Configuration(context.Configuration).WriteTo.Console());
        builder.Services.AddProblemDetails();
        builder.Services.AddInfrastructure(builder.Configuration);
        return builder;
    }

    public static WebApplication UseKnowledgeAppBootstrap(this WebApplication app)
    {
        app.UseExceptionHandler();
        app.UseStatusCodePages();
        return app;
    }
}
