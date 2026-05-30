using System.Text.RegularExpressions;
using KnowledgeApp.Application.Abstractions;
using KnowledgeApp.Application.Common.Results;
using KnowledgeApp.Contracts.Search;
using KnowledgeApp.Domain.Enums;

namespace KnowledgeApp.Application.Search;

public sealed class ContentSearchHandler(
    IChunkSearchIndex chunkSearchIndex,
    ContentSearchRequestValidator validator)
{
    private const int MaxCandidateCount = 500;
    private const int SnippetLength = 220;

    public async Task<Result<ContentSearchResponse>> HandleAsync(
        ContentSearchRequest request,
        CancellationToken cancellationToken = default)
    {
        Result validation = validator.Validate(request);
        if (!validation.IsSuccess)
        {
            return Result<ContentSearchResponse>.Failure(validation);
        }

        string phrase = request.Query.Trim();
        string[] terms = ExtractTerms(phrase);

        List<ContentSearchHitDto> hits = [];

        if (request.IncludeDocuments)
        {
            IReadOnlyList<ContentSearchHitDto> documentHits =
                await SearchDocumentsAsync(request, phrase, terms, cancellationToken);

            hits.AddRange(documentHits);
        }

        if (request.IncludeNotes)
        {
            IReadOnlyList<ContentSearchHitDto> noteHits =
                await SearchNotesAsync(request, phrase, terms, cancellationToken);

            hits.AddRange(noteHits);
        }

        ContentSearchHitDto[] results = hits
            .OrderByDescending(hit => hit.Score)
            .ThenBy(hit => hit.SourceType, StringComparer.Ordinal)
            .ThenBy(hit => hit.Title, StringComparer.OrdinalIgnoreCase)
            .Take(request.Limit)
            .ToArray();

        return Result<ContentSearchResponse>.Success(new ContentSearchResponse(results));
    }

    private async Task<IReadOnlyList<ContentSearchHitDto>> SearchDocumentsAsync(
        ContentSearchRequest request,
        string phrase,
        IReadOnlyList<string> terms,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<DocumentChunkCandidate> candidates = await chunkSearchIndex.SearchDocumentCandidatesAsync(
            terms.ToArray(),
            request.BucketId,
            request.DocumentId,
            MaxCandidateCount,
            cancellationToken);

        return candidates
            .Select(candidate => new ContentSearchHitDto(
                SourceType: "document",
                SourceId: candidate.DocumentId,
                ChunkId: candidate.ChunkId,
                Title: candidate.Title,
                PageNumber: candidate.PageNumber,
                Score: CalculateScore(candidate.Title, candidate.Text, phrase, terms),
                Snippet: BuildSnippet(candidate.Text, phrase, terms)))
            .ToArray();
    }

    private async Task<IReadOnlyList<ContentSearchHitDto>> SearchNotesAsync(
        ContentSearchRequest request,
        string phrase,
        IReadOnlyList<string> terms,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<NoteCandidate> candidates = await chunkSearchIndex.SearchNoteCandidatesAsync(
            terms.ToArray(),
            request.BucketId,
            request.NoteId,
            MaxCandidateCount,
            cancellationToken);

        return candidates
            .Select(candidate => new ContentSearchHitDto(
                SourceType: "note",
                SourceId: candidate.NoteId,
                ChunkId: null,
                Title: candidate.Title,
                PageNumber: null,
                Score: CalculateScore(candidate.Title, candidate.Markdown, phrase, terms),
                Snippet: BuildSnippet(candidate.Markdown, phrase, terms)))
            .ToArray();
    }

    private static string[] ExtractTerms(string phrase)
    {
        string[] terms = Regex
            .Matches(phrase, @"[\p{L}\p{N}]+")
            .Select(match => match.Value)
            .Where(term => term.Length > 1)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return terms.Length == 0 ? [phrase] : terms;
    }

    private static double CalculateScore(
        string title,
        string text,
        string phrase,
        IReadOnlyList<string> terms)
    {
        double score = 0;

        if (title.Equals(phrase, StringComparison.OrdinalIgnoreCase))
        {
            score += 100;
        }

        score += CountOccurrences(title, phrase) * 40;
        score += CountOccurrences(text, phrase) * 20;

        foreach (string term in terms)
        {
            score += CountOccurrences(title, term) * 8;
            score += Math.Min(CountOccurrences(text, term), 5) * 2;
        }

        return score;
    }

    private static int CountOccurrences(string text, string value)
    {
        if (string.IsNullOrWhiteSpace(text) || string.IsNullOrWhiteSpace(value))
        {
            return 0;
        }

        int count = 0;
        int currentIndex = 0;

        while (true)
        {
            int matchIndex = text.IndexOf(
                value,
                currentIndex,
                StringComparison.OrdinalIgnoreCase);

            if (matchIndex < 0)
            {
                return count;
            }

            count++;
            currentIndex = matchIndex + value.Length;
        }
    }

    private static string BuildSnippet(
        string text,
        string phrase,
        IReadOnlyList<string> terms)
    {
        string normalizedText = Regex.Replace(text, @"\s+", " ").Trim();

        if (normalizedText.Length <= SnippetLength)
        {
            return normalizedText;
        }

        int matchIndex = normalizedText.IndexOf(
            phrase,
            StringComparison.OrdinalIgnoreCase);

        if (matchIndex < 0)
        {
            foreach (string term in terms)
            {
                matchIndex = normalizedText.IndexOf(
                    term,
                    StringComparison.OrdinalIgnoreCase);

                if (matchIndex >= 0)
                {
                    break;
                }
            }
        }

        if (matchIndex < 0)
        {
            matchIndex = 0;
        }

        int startIndex = Math.Max(0, matchIndex - 60);
        int length = Math.Min(SnippetLength, normalizedText.Length - startIndex);

        string snippet = normalizedText
            .Substring(startIndex, length)
            .Trim();

        string prefix = startIndex > 0 ? "…" : string.Empty;
        string suffix = startIndex + length < normalizedText.Length ? "…" : string.Empty;

        return prefix + snippet + suffix;
    }
}
