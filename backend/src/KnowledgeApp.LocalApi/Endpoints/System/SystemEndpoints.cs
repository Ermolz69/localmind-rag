using KnowledgeApp.Contracts.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;

namespace KnowledgeApp.LocalApi.Endpoints;

public static class SystemEndpoints
{
    public static IEndpointRouteBuilder MapSystemEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapGroup("/system")
            .WithTags("System");

        group.MapPost("/shutdown", (IHostApplicationLifetime lifetime, HttpContext context) =>
        {
            lifetime.StopApplication();
            return ApiResults.Accepted<object?>(null, null, context);
        })
        .WithName("ShutdownSystem")
        .WithSummary("Gracefully shuts down the LocalApi backend.")
        .Produces<ApiResponse<object?>>(StatusCodes.Status202Accepted);

        return app;
    }
}
