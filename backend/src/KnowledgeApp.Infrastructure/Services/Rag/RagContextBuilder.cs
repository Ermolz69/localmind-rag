using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

using KnowledgeApp.Application.Abstractions;
using KnowledgeApp.Application.Common.Diagnostics;
using KnowledgeApp.Contracts.Rag;
using KnowledgeApp.Infrastructure.Options;

using Microsoft.Extensions.Options;

namespace KnowledgeApp.Infrastructure.Services;

public sealed class RagContextBuilder(
    IHybridRetrievalService search,
    IEmbeddingGenerator embeddings,
    IOptions<RagOptions> options,
    IAppDiagnosticLogger? diagnostics = null) : IRagContextBuilder
{
    private const int StrongKeywordRankCutoff = 10;
    private const int SnippetCharacterLimit = 700;
    private const int ContextCharacterLimit = 6_000;

    private static readonly HashSet<string> KeywordGuardStopWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "about",
        "after",
        "before",
        "company",
        "does",
        "employee",
        "employees",
        "and",
        "are",
        "for",
        "from",
        "have",
        "how",
        "into",
        "that",
        "the",
        "this",
        "through",
        "required",
        "requires",
        "should",
        "was",
        "what",
        "when",
        "where",
        "which",
        "with"
    };

    private readonly RagOptions options = options.Value;

    public async Task<RagContext> BuildAsync(
        RagContextRequest request,
        CancellationToken cancellationToken = default)
    {
        Guid operationId = diagnostics?.BeginOperation(
            DiagnosticNames.Areas.Rag,
            DiagnosticNames.Operations.BuildContext,
            new Dictionary<string, object?>
            {
                [DiagnosticNames.Properties.ConversationId] = request.ConversationId,
                [DiagnosticNames.Properties.Limit] = request.Limit,
            }) ?? Guid.Empty;

        float[] queryVector =
            await embeddings.GenerateAsync(request.Question, cancellationToken);

        IReadOnlyList<HybridSearchResult> rankedSources = await search.SearchAsync(
            request.Question,
            queryVector,
            new HybridSearchOptions(Limit: request.Limit),
            cancellationToken);

        IReadOnlyList<RagSourceDto> relevantSources = FilterRelevantSources(
            rankedSources,
            request.Question);

        string contextText = BuildContextText(relevantSources, request.Question);

        diagnostics?.LogStep(
            operationId,
            DiagnosticNames.Steps.ContextBuilt,
            new Dictionary<string, object?>
            {
                [DiagnosticNames.Properties.SourcesCount] = relevantSources.Count,
                [DiagnosticNames.Properties.ContextLength] = contextText.Length,
            });

        return new RagContext(relevantSources, contextText);
    }

    private IReadOnlyList<RagSourceDto> FilterRelevantSources(
        IReadOnlyList<HybridSearchResult> rankedSources,
        string question)
    {
        string[] distinctiveTerms = ExtractDistinctiveTerms(question);

        return rankedSources
            .Where(source => IsRelevantForContext(source, distinctiveTerms))
            .Select(source => source.ToRagSource())
            .ToArray();
    }

    private bool IsRelevantForContext(
        HybridSearchResult source,
        IReadOnlyList<string> distinctiveTerms)
    {
        if (source.VectorScore is { } vectorScore && vectorScore >= options.MinimumSourceScore)
        {
            return true;
        }

        if (source.FullTextRank is not { } fullTextRank || fullTextRank > StrongKeywordRankCutoff)
        {
            return false;
        }

        return HasStrongKeywordOverlap(source.Snippet, distinctiveTerms);
    }

    private static bool HasStrongKeywordOverlap(
        string snippet,
        IReadOnlyList<string> distinctiveTerms)
    {
        if (distinctiveTerms.Count == 0)
        {
            return false;
        }

        int matchCount = distinctiveTerms.Count(term =>
            snippet.Contains(term, StringComparison.OrdinalIgnoreCase));

        int requiredMatches = distinctiveTerms.Count == 1 ? 1 : 2;

        return matchCount >= requiredMatches;
    }

    private static string[] ExtractDistinctiveTerms(string question)
    {
        return Regex
            .Matches(question, @"[\p{L}\p{N}]+")
            .Select(match => match.Value)
            .Where(term => term.Length > 2)
            .Where(term => !KeywordGuardStopWords.Contains(term))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static string BuildContextText(
        IReadOnlyList<RagSourceDto> sources,
        string question)
    {
        if (sources.Count == 0)
        {
            return string.Empty;
        }

        StringBuilder builder = new();

        for (int index = 0; index < sources.Count; index++)
        {
            RagSourceDto source = sources[index];

            string snippet = BuildFocusedSnippet(source.Snippet, question);

            string page = source.PageNumber.HasValue
                ? source.PageNumber.Value.ToString(CultureInfo.InvariantCulture)
                : "n/a";

            builder.Append(
                CultureInfo.InvariantCulture,
                $"[Source {index + 1}] ");

            builder.Append(
                CultureInfo.InvariantCulture,
                $"Document: {source.DocumentName}; ");

            builder.Append(
                CultureInfo.InvariantCulture,
                $"DocumentId: {source.DocumentId}; ");

            builder.Append(
                CultureInfo.InvariantCulture,
                $"ChunkId: {source.ChunkId}; ");

            builder.Append(
                CultureInfo.InvariantCulture,
                $"Page: {page}; ");

            builder.Append(
                CultureInfo.InvariantCulture,
                $"Score: {source.Score:0.0000}; ");

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

    private static string BuildFocusedSnippet(string text, string question)
    {
        string normalizedText = NormalizeSnippet(text);

        if (normalizedText.Length <= SnippetCharacterLimit)
        {
            return normalizedText;
        }

        int matchIndex = FindBestMatchIndex(normalizedText, question);
        int startIndex = matchIndex < 0
            ? 0
            : Math.Max(0, matchIndex - (SnippetCharacterLimit / 3));

        if (startIndex + SnippetCharacterLimit > normalizedText.Length)
        {
            startIndex = Math.Max(0, normalizedText.Length - SnippetCharacterLimit);
        }

        string snippet = normalizedText
            .Substring(startIndex, Math.Min(SnippetCharacterLimit, normalizedText.Length - startIndex))
            .Trim();

        string prefix = startIndex > 0 ? "..." : string.Empty;
        string suffix = startIndex + snippet.Length < normalizedText.Length ? "..." : string.Empty;

        return prefix + snippet + suffix;
    }

    private static int FindBestMatchIndex(string text, string question)
    {
        string phrase = NormalizeSnippet(question)
            .Trim('?', '!', '.', ':', ';', ',', ' ', '\t');

        if (!string.IsNullOrWhiteSpace(phrase))
        {
            int phraseIndex = text.IndexOf(phrase, StringComparison.OrdinalIgnoreCase);
            if (phraseIndex >= 0)
            {
                return phraseIndex;
            }
        }

        string[] terms = Regex
            .Matches(question, @"[\p{L}\p{N}]+")
            .Select(match => match.Value)
            .Where(term => term.Length > 2)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(term => term.Length)
            .ToArray();

        foreach (string term in terms)
        {
            int termIndex = text.IndexOf(term, StringComparison.OrdinalIgnoreCase);
            if (termIndex >= 0)
            {
                return termIndex;
            }
        }

        return -1;
    }
}
