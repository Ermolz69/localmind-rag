using System.Text;
using System.Text.RegularExpressions;
using KnowledgeApp.Application.Abstractions.Ingestion;

namespace KnowledgeApp.Infrastructure.Services.Ingestion.Chunking;

public sealed partial class ChunkTextNormalizer : IChunkTextNormalizer
{
    public string NormalizeForEmbedding(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        string normalized = text.Normalize(NormalizationForm.FormC);
        normalized = normalized.Replace("\r\n", "\n").Replace('\r', '\n');
        normalized = WhitespaceRegex().Replace(normalized, " ");
        normalized = RepeatedNewlinesRegex().Replace(normalized, "\n\n");

        return normalized.Trim();
    }

    public string NormalizeForIdentity(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        string normalized = text.Normalize(NormalizationForm.FormC);
        normalized = normalized.Replace("\r\n", "\n").Replace('\r', '\n');

        return normalized.Trim();
    }

    [GeneratedRegex(@"[^\S\r\n]+")]
    private static partial Regex WhitespaceRegex();

    [GeneratedRegex(@"\n{3,}")]
    private static partial Regex RepeatedNewlinesRegex();
}
