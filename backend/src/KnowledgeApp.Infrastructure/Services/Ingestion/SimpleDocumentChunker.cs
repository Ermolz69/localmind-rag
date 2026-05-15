using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using DocumentFormat.OpenXml.Packaging;
using KnowledgeApp.Application.Abstractions;
using KnowledgeApp.Contracts.Documents;
using KnowledgeApp.Contracts.Rag;
using KnowledgeApp.Contracts.Runtime;
using KnowledgeApp.Domain.Entities;
using KnowledgeApp.Domain.Enums;
using KnowledgeApp.Infrastructure.Options;
using KnowledgeApp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using UglyToad.PdfPig;
using A = DocumentFormat.OpenXml.Drawing;
using PresentationSlideId = DocumentFormat.OpenXml.Presentation.SlideId;
using SlideText = DocumentFormat.OpenXml.Drawing.Text;
using WordParagraph = DocumentFormat.OpenXml.Wordprocessing.Paragraph;
using WordText = DocumentFormat.OpenXml.Wordprocessing.Text;

namespace KnowledgeApp.Infrastructure.Services;

public sealed class SimpleDocumentChunker : IDocumentChunker
{
    private const int TargetChunkSize = 1200;

    public IReadOnlyList<string> Split(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return [];
        }

        var normalizedText = text.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n');
        var paragraphs = normalizedText
            .Split("\n\n", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(paragraph => Regex.Replace(paragraph, @"\s+", " ").Trim())
            .Where(paragraph => paragraph.Length > 0);

        var chunks = new List<string>();
        var current = new StringBuilder();
        foreach (var paragraph in paragraphs)
        {
            if (paragraph.Length > TargetChunkSize)
            {
                FlushCurrentChunk(chunks, current);
                chunks.AddRange(SplitLongParagraph(paragraph));
                continue;
            }

            if (current.Length > 0 && current.Length + 2 + paragraph.Length > TargetChunkSize)
            {
                FlushCurrentChunk(chunks, current);
            }

            if (current.Length > 0)
            {
                current.Append("\n\n");
            }

            current.Append(paragraph);
        }

        FlushCurrentChunk(chunks, current);
        return chunks;
    }

    private static IEnumerable<string> SplitLongParagraph(string paragraph)
    {
        for (var index = 0; index < paragraph.Length; index += TargetChunkSize)
        {
            yield return paragraph.Substring(index, Math.Min(TargetChunkSize, paragraph.Length - index));
        }
    }

    private static void FlushCurrentChunk(ICollection<string> chunks, StringBuilder current)
    {
        if (current.Length == 0)
        {
            return;
        }

        chunks.Add(current.ToString());
        current.Clear();
    }
}
