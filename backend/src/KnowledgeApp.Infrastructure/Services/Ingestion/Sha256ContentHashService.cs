using System.Security.Cryptography;
using System.Text;
using KnowledgeApp.Application.Ingestion.IncrementalIndexing;

namespace KnowledgeApp.Infrastructure.Services;

public sealed class Sha256ContentHashService : IContentHashService
{
    public string ComputeChunkHash(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        string normalizedText = NormalizeText(text);

        return ComputeSha256Hex(normalizedText);
    }

    public string ComputeDocumentHash(IEnumerable<string> orderedChunkHashes, int indexVersion)
    {
        ArgumentNullException.ThrowIfNull(orderedChunkHashes);

        StringBuilder builder = new StringBuilder();

        builder.Append("index-version:");
        builder.Append(indexVersion);
        builder.Append('\n');

        foreach (string chunkHash in orderedChunkHashes)
        {
            builder.Append(chunkHash);
            builder.Append('\n');
        }

        return ComputeSha256Hex(builder.ToString());
    }

    private static string NormalizeText(string text)
    {
        return text
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace('\r', '\n');
    }

    private static string ComputeSha256Hex(string value)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(value);
        byte[] hash = SHA256.HashData(bytes);

        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
