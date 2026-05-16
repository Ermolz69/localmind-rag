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

internal static class ExtractedText
{
    public static DocumentTextExtractionResult FromSingle(string text, string? sectionTitle = null, string sourceKind = "Document")
    {
        return FromSegments([new DocumentTextSegment(text, null, sectionTitle, sourceKind)]);
    }

    public static DocumentTextExtractionResult FromSegments(IEnumerable<DocumentTextSegment> segments)
    {
        DocumentTextSegment[]? cleanSegments = segments
            .Select(segment => segment with { Text = segment.Text.Trim() })
            .Where(segment => segment.Text.Length > 0)
            .ToArray();

        if (cleanSegments.Length == 0)
        {
            throw new InvalidOperationException("No extractable text was found in the document.");
        }

        return new DocumentTextExtractionResult(cleanSegments);
    }
}
