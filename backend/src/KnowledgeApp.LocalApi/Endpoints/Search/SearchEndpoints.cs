using KnowledgeApp.Application.Search;
using KnowledgeApp.Contracts.Rag;

namespace KnowledgeApp.LocalApi.Endpoints;

public static class SearchEndpoints
{
    public static IEndpointRouteBuilder MapSearchEndpoints(this IEndpointRouteBuilder app)
    {
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
