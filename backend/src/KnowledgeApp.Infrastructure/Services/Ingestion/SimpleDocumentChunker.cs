using System.Text;
using System.Text.RegularExpressions;
using KnowledgeApp.Application.Abstractions;
using KnowledgeApp.Infrastructure.Options;
using Microsoft.Extensions.Options;

namespace KnowledgeApp.Infrastructure.Services;

public sealed class SimpleDocumentChunker : IDocumentChunker
{
    private readonly ChunkingOptions options;

    public SimpleDocumentChunker()
        : this(Microsoft.Extensions.Options.Options.Create(new ChunkingOptions()))
    {
    }

    public SimpleDocumentChunker(IOptions<ChunkingOptions> options)
    {
        this.options = options.Value;
    }

    public IReadOnlyList<string> Split(string text)
    {
        return SplitDetailed(text)
            .Select(chunk => chunk.Text)
            .ToArray();
    }

    public IReadOnlyList<DocumentChunkText> SplitDetailed(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return [];
        }

        string normalizedText = text.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n');
        IReadOnlyList<TextBlock> blocks = BuildBlocks(normalizedText);
        List<DocumentChunkText> chunks = [];
        List<TextBlock> currentBlocks = [];
        int currentLength = 0;

        foreach (TextBlock block in blocks)
        {
            if (block.Text.Length > options.MaxChunkCharacters)
            {
                FlushCurrent(chunks, currentBlocks, ref currentLength);
                chunks.AddRange(SplitForced(block));
                continue;
            }

            int separatorLength = currentBlocks.Count == 0 ? 0 : 2;
            if (currentBlocks.Count > 0 &&
                currentLength + separatorLength + block.Text.Length > options.TargetChunkCharacters &&
                currentLength >= options.MinChunkCharacters)
            {
                FlushCurrent(chunks, currentBlocks, ref currentLength);
                separatorLength = 0;
            }

            currentBlocks.Add(block);
            currentLength += separatorLength + block.Text.Length;
        }

        FlushCurrent(chunks, currentBlocks, ref currentLength);

        return chunks;
    }

    private IReadOnlyList<TextBlock> BuildBlocks(string text)
    {
        List<TextBlock> blocks = [];
        List<string> headingPath = [];
        MatchCollection matches = Regex.Matches(text, @"(?ms)(^#{1,6}\s+.+?$)|(.+?)(?=\n\s*\n|\z)");

        foreach (Match match in matches)
        {
            string raw = match.Value.Trim();
            if (raw.Length == 0)
            {
                continue;
            }

            Match heading = Regex.Match(raw, @"^(#{1,6})\s+(.+)$");
            if (heading.Success)
            {
                int level = heading.Groups[1].Value.Length;
                string title = NormalizeInlineText(heading.Groups[2].Value);

                while (headingPath.Count >= level)
                {
                    headingPath.RemoveAt(headingPath.Count - 1);
                }

                headingPath.Add(title);

                if (options.PreserveHeadings)
                {
                    blocks.Add(new TextBlock(raw, string.Join(" > ", headingPath), match.Index, match.Index + match.Length));
                }

                continue;
            }

            string normalized = NormalizeBlock(raw);
            if (normalized.Length == 0)
            {
                continue;
            }

            blocks.Add(new TextBlock(
                normalized,
                headingPath.Count == 0 ? null : string.Join(" > ", headingPath),
                match.Index,
                match.Index + match.Length));
        }

        return blocks;
    }

    private IEnumerable<DocumentChunkText> SplitForced(TextBlock block)
    {
        foreach (string piece in SplitBySentences(block.Text))
        {
            int offset = block.Text.IndexOf(piece, StringComparison.Ordinal);
            int sourceStart = offset < 0 ? block.StartOffset : block.StartOffset + offset;

            yield return new DocumentChunkText(
                piece,
                block.HeadingPath,
                sourceStart,
                sourceStart + piece.Length);
        }
    }

    private IReadOnlyList<string> SplitBySentences(string text)
    {
        string[] sentences = Regex.Split(text, @"(?<=[.!?])\s+")
            .Where(sentence => !string.IsNullOrWhiteSpace(sentence))
            .Select(sentence => sentence.Trim())
            .ToArray();

        if (sentences.Length <= 1)
        {
            return SplitByCharacters(text);
        }

        List<string> chunks = [];
        StringBuilder current = new();

        foreach (string sentence in sentences)
        {
            if (sentence.Length > options.MaxChunkCharacters)
            {
                FlushText(chunks, current);
                chunks.AddRange(SplitByCharacters(sentence));
                continue;
            }

            if (current.Length > 0 && current.Length + 1 + sentence.Length > options.TargetChunkCharacters)
            {
                FlushText(chunks, current);
            }

            if (current.Length > 0)
            {
                current.Append(' ');
            }

            current.Append(sentence);
        }

        FlushText(chunks, current);
        return chunks;
    }

    private IReadOnlyList<string> SplitByCharacters(string text)
    {
        List<string> chunks = [];
        int step = options.ApplyOverlapOnlyOnForcedSplit
            ? Math.Max(1, options.TargetChunkCharacters - options.OverlapCharacters)
            : options.TargetChunkCharacters;

        for (int index = 0; index < text.Length; index += step)
        {
            int length = Math.Min(options.TargetChunkCharacters, text.Length - index);
            chunks.Add(text.Substring(index, length));
        }

        return chunks;
    }

    private static void FlushCurrent(
        ICollection<DocumentChunkText> chunks,
        List<TextBlock> currentBlocks,
        ref int currentLength)
    {
        if (currentBlocks.Count == 0)
        {
            return;
        }

        string text = string.Join("\n\n", currentBlocks.Select(block => block.Text));
        string? headingPath = currentBlocks.LastOrDefault(block => !string.IsNullOrWhiteSpace(block.HeadingPath))?.HeadingPath;

        chunks.Add(new DocumentChunkText(
            text,
            headingPath,
            currentBlocks[0].StartOffset,
            currentBlocks[^1].EndOffset));

        currentBlocks.Clear();
        currentLength = 0;
    }

    private static void FlushText(ICollection<string> chunks, StringBuilder current)
    {
        if (current.Length == 0)
        {
            return;
        }

        chunks.Add(current.ToString());
        current.Clear();
    }

    private static string NormalizeBlock(string text)
    {
        string normalizedLines = Regex.Replace(text.Trim(), @"[ \t]+", " ");
        return Regex.Replace(normalizedLines, @"\n{3,}", "\n\n").Trim();
    }

    private static string NormalizeInlineText(string text)
    {
        return Regex.Replace(text.Trim(), @"\s+", " ");
    }

    private sealed record TextBlock(
        string Text,
        string? HeadingPath,
        int StartOffset,
        int EndOffset);
}
