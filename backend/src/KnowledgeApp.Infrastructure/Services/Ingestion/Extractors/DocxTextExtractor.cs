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

public sealed class DocxTextExtractor : IDocumentTextExtractor
{
    public Task<DocumentTextExtractionResult> ExtractAsync(string filePath, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            FileSignatureValidator.EnsureZipPackage(filePath, "DOCX");
            using var document = WordprocessingDocument.Open(filePath, false);
            var body = document.MainDocumentPart?.Document?.Body;
            if (body is null)
            {
                throw new InvalidOperationException("DOCX document body was not found.");
            }

            var paragraphs = body
                .Descendants<WordParagraph>()
                .Select(ExtractWordParagraphText)
                .Where(paragraph => paragraph.Length > 0);

            return Task.FromResult(ExtractedText.FromSingle(string.Join("\n\n", paragraphs), "Document", "DocxDocument"));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (InvalidOperationException)
        {
            throw;
        }
        catch (Exception exception)
        {
            throw new InvalidOperationException($"Failed to extract text from DOCX document: {exception.Message}", exception);
        }
    }

    private static string ExtractWordParagraphText(WordParagraph paragraph)
    {
        var text = string.Concat(paragraph.Descendants<WordText>().Select(text => text.Text)).Trim();
        var style = paragraph.ParagraphProperties?.ParagraphStyleId?.Val?.Value;
        return string.IsNullOrWhiteSpace(style) ? text : $"{style}: {text}";
    }
}
