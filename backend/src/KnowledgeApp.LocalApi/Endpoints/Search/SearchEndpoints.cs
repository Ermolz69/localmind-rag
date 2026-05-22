using KnowledgeApp.Application.Search;
using KnowledgeApp.Contracts.Rag;
using KnowledgeApp.Contracts.Search;

namespace KnowledgeApp.LocalApi.Endpoints;

public static class SearchEndpoints
{
    public static IEndpointRouteBuilder MapSearchEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/search/content", async (
            ContentSearchRequest request,
            ContentSearchHandler handler,
            CancellationToken cancellationToken) =>
        {
            ContentSearchResponse response = await handler.HandleAsync(request, cancellationToken);
            return Results.Ok(response);
        });

        app.MapPost("/api/search/semantic", async (
            SemanticSearchRequest request,
            SemanticSearchHandler handler,
            CancellationToken cancellationToken) =>
        {
            SemanticSearchResponse response = await handler.HandleAsync(request, cancellationToken);
            return Results.Ok(response);
        });

        return app;
    }
}
