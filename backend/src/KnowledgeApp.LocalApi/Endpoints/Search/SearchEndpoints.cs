using KnowledgeApp.Application.Search;
using KnowledgeApp.Contracts.Common;
using KnowledgeApp.Contracts.Rag;
using KnowledgeApp.Contracts.Search;

namespace KnowledgeApp.LocalApi.Endpoints;

public static class SearchEndpoints
{
    public static IEndpointRouteBuilder MapSearchEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/search/content", async (
            ContentSearchRequest request,
            ContentSearchHandler handler,
            HttpContext context,
            CancellationToken cancellationToken) =>
        {
            return (await handler.HandleAsync(request, cancellationToken)).ToApiResult(context);
        })
            .WithName("ContentSearch")
            .WithTags("Search")
            .WithSummary("Runs content search.")
            .WithDescription("Searches document chunks and notes with text matching across selected sources.")
            .Produces<ApiResponse<ContentSearchResponse>>()
            .Produces<ApiResponse<object?>>(StatusCodes.Status400BadRequest);

        app.MapPost("/search/semantic", async (
            SemanticSearchRequest request,
            SemanticSearchHandler handler,
            HttpContext context,
            CancellationToken cancellationToken) =>
        {
            return (await handler.HandleAsync(request, cancellationToken)).ToApiResult(context);
        })
            .WithName("SemanticSearch")
            .WithTags("Search")
            .WithSummary("Runs semantic search.")
            .WithDescription("Searches indexed document chunks by semantic similarity with optional bucket or document scope.")
            .Produces<ApiResponse<SemanticSearchResponse>>()
            .Produces<ApiResponse<object?>>(StatusCodes.Status400BadRequest);

        return app;
    }
}
