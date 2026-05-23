using KnowledgeApp.Application.Abstractions;
using KnowledgeApp.Application.Common.Diagnostics;
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
            DiagnosticNames.Areas.Search,
            DiagnosticNames.Operations.SemanticSearch,
            new Dictionary<string, object?>
            {
                [DiagnosticNames.Properties.Limit] = request.Limit,
                [DiagnosticNames.Properties.BucketId] = request.BucketId,
                [DiagnosticNames.Properties.DocumentId] = request.DocumentId,
            }) ?? Guid.Empty;

        try
        {
            diagnostics?.LogStep(operationId, DiagnosticNames.Steps.SemanticSearchStarted);
            validator.Validate(request);

            float[] vector = await embeddings.GenerateAsync(request.Query.Trim(), cancellationToken);
            VectorSearchOptions options = new(request.Limit, request.BucketId, request.DocumentId);
            IReadOnlyList<RagSourceDto> sources = await search.SearchAsync(vector, options, cancellationToken);

            diagnostics?.LogStep(
                operationId,
                DiagnosticNames.Steps.SemanticSearchCompleted,
                new Dictionary<string, object?>
                {
                    [DiagnosticNames.Properties.SourcesCount] = sources.Count,
                });

            return new SemanticSearchResponse(sources);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            diagnostics?.LogFailure(operationId, exception);
            diagnostics?.LogStep(operationId, DiagnosticNames.Steps.SemanticSearchFailed);
            throw;
        }
    }
}
