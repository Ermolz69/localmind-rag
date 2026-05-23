using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using KnowledgeApp.Application.Abstractions;
using KnowledgeApp.Contracts.Rag;

namespace KnowledgeApp.Infrastructure.Services;

public sealed class RagContextBuilder(
    IVectorSearchService search,
    IEmbeddingGenerator embeddings,
    IAppDiagnosticLogger? diagnostics = null) : IRagContextBuilder
{
    private const int SnippetCharacterLimit = 700;
    private const int ContextCharacterLimit = 6_000;

    public async Task<RagContext> BuildAsync(RagContextRequest request, CancellationToken cancellationToken = default)
    {
        Guid operationId = diagnostics?.BeginOperation(
            "rag",
            "build-context",
            new Dictionary<string, object?>
            {
                ["ConversationId"] = request.ConversationId,
                ["Limit"] = request.Limit,
            }) ?? Guid.Empty;

        float[] queryVector = await embeddings.GenerateAsync(request.Question, cancellationToken);
        IReadOnlyList<RagSourceDto> sources = await search.SearchAsync(
            queryVector,
            new VectorSearchOptions(Limit: request.Limit),
            cancellationToken);

        string contextText = BuildContextText(sources);
        diagnostics?.LogStep(
            operationId,
            "context-built",
            new Dictionary<string, object?>
            {
                ["SourcesCount"] = sources.Count,
                ["ContextLength"] = contextText.Length,
            });
        return new RagContext(sources, contextText);
    }

    private static string BuildContextText(IReadOnlyList<RagSourceDto> sources)
    {
        if (sources.Count == 0)
        {
            return string.Empty;
        }

        StringBuilder builder = new();
        for (int index = 0; index < sources.Count; index++)
        {
            RagSourceDto source = sources[index];
            string snippet = NormalizeSnippet(source.Snippet);
            if (snippet.Length > SnippetCharacterLimit)
            {
                snippet = snippet[..SnippetCharacterLimit];
            }

            string page = source.PageNumber.HasValue
                ? source.PageNumber.Value.ToString(CultureInfo.InvariantCulture)
                : "n/a";

            builder.Append(CultureInfo.InvariantCulture, $"[Source {index + 1}] ");
            builder.Append(CultureInfo.InvariantCulture, $"Document: {source.DocumentName}; ");
            builder.Append(CultureInfo.InvariantCulture, $"DocumentId: {source.DocumentId}; ");
            builder.Append(CultureInfo.InvariantCulture, $"ChunkId: {source.ChunkId}; ");
            builder.Append(CultureInfo.InvariantCulture, $"Page: {page}; ");
            builder.Append(CultureInfo.InvariantCulture, $"Score: {source.Score:0.0000}; ");
            builder.Append("Snippet: ");
            builder.AppendLine(snippet);

            if (builder.Length >= ContextCharacterLimit)
            {
                break;
            }
        }

        if (builder.Length <= ContextCharacterLimit)
        {
            return builder.ToString().Trim();
        }

        return builder.ToString(0, ContextCharacterLimit).Trim();
    }

    private static string NormalizeSnippet(string snippet)
    {
        return Regex.Replace(snippet, "\\s+", " ").Trim();
    }
}
