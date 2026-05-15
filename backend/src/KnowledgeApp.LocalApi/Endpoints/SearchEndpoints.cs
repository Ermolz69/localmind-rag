using KnowledgeApp.Application.Abstractions;
using KnowledgeApp.Contracts.Rag;

namespace KnowledgeApp.LocalApi.Endpoints;

public static class SearchEndpoints
{
    public static IEndpointRouteBuilder MapSearchEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/search/semantic", async (
            SemanticSearchRequest request,
            IEmbeddingGenerator embeddings,
            IVectorSearchService search,
            CancellationToken cancellationToken) =>
        {
            var vector = await embeddings.GenerateAsync(request.Query, cancellationToken);
            var options = new VectorSearchOptions(request.Limit, request.BucketId, request.DocumentId);
            return Results.Ok(await search.SearchAsync(vector, options, cancellationToken));
        });

        return app;
    }
}
