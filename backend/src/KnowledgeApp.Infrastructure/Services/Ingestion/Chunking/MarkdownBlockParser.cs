using System.Text.RegularExpressions;
using KnowledgeApp.Application.Abstractions.Ingestion;

namespace KnowledgeApp.Infrastructure.Services.Ingestion.Chunking;

public sealed partial class MarkdownBlockParser(ITokenizerService tokenizer) : ITextStructureParser
{
    public bool CanParse(string text)
    {
        return true; // Simple fallback or explicitly markdown for MVP of this parser
    }

    public IReadOnlyList<DocumentBlock> Parse(string text)
    {
        List<DocumentBlock> blocks = [];

        if (string.IsNullOrWhiteSpace(text))
        {
            return blocks;
        }

        // Extremely simplified parser for MVP. In production, we'd use Markdig.
        // We'll treat paragraphs separated by \n\n as blocks.
        string[] paragraphs = text.Split("\n\n", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        int currentOffset = 0;

        foreach (string paragraph in paragraphs)
        {
            int tokenCount = tokenizer.CountTokens(paragraph);

            blocks.Add(new DocumentBlock(
                Type: DocumentBlockType.Paragraph,
                Text: paragraph,
                HeadingPath: null,
                SectionTitle: null,
                SourceStartOffset: currentOffset,
                SourceEndOffset: currentOffset + paragraph.Length,
                TokenCount: tokenCount,
                IsAtomic: false));

            currentOffset += paragraph.Length + 2; // approximation for offsets
        }

        return blocks;
    }
}
