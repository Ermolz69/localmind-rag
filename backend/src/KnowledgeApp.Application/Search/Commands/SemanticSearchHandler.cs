using KnowledgeApp.Application.Abstractions;
using KnowledgeApp.Contracts.Rag;

namespace KnowledgeApp.Application.Search;

public sealed class SemanticSearchHandler(
    IEmbeddingGenerator embeddings,
    IVectorSearchService search,
    SemanticSearchRequestValidator validator,
    IAppDiagnosticLogger? diagnostics = null)
{
    public async Task<SemanticSearchResponse> HandleAsync(
        SemanticSearchRequest request,
        CancellationToken cancellationToken = default)
    {
        Guid operationId = diagnostics?.BeginOperation(
            "search",
            "semantic",
            new Dictionary<string, object?>
            {
                ["Limit"] = request.Limit,
                ["BucketId"] = request.BucketId,
                ["DocumentId"] = request.DocumentId,
            }) ?? Guid.Empty;

        validator.Validate(request);

        float[] vector = await embeddings.GenerateAsync(request.Query.Trim(), cancellationToken);
        VectorSearchOptions options = new(request.Limit, request.BucketId, request.DocumentId);
        IReadOnlyList<RagSourceDto> sources = await search.SearchAsync(vector, options, cancellationToken);

        diagnostics?.LogStep(
            operationId,
            "semantic-search-completed",
            new Dictionary<string, object?>
            {
                ["SourcesCount"] = sources.Count,
            });

        return new SemanticSearchResponse(sources);
    }
}
