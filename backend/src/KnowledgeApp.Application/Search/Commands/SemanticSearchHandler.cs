using KnowledgeApp.Application.Abstractions;
using KnowledgeApp.Contracts.Rag;

namespace KnowledgeApp.Application.Search;

public sealed class SemanticSearchHandler(
    IEmbeddingGenerator embeddings,
    IVectorSearchService search,
    SemanticSearchRequestValidator validator)
{
    public async Task<SemanticSearchResponse> HandleAsync(
        SemanticSearchRequest request,
        CancellationToken cancellationToken = default)
    {
        validator.Validate(request);

        float[] vector = await embeddings.GenerateAsync(request.Query.Trim(), cancellationToken);
        VectorSearchOptions options = new(request.Limit, request.BucketId, request.DocumentId);
        IReadOnlyList<RagSourceDto> sources = await search.SearchAsync(vector, options, cancellationToken);

        return new SemanticSearchResponse(sources);
    }
}
