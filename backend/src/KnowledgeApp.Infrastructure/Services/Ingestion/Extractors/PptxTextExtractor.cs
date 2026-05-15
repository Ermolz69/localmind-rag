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

public sealed class PptxTextExtractor : IDocumentTextExtractor
{
    public Task<DocumentTextExtractionResult> ExtractAsync(string filePath, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            FileSignatureValidator.EnsureZipPackage(filePath, "PPTX");
            using var document = PresentationDocument.Open(filePath, false);
            var presentationPart = document.PresentationPart;
            var slideIdList = presentationPart?.Presentation?.SlideIdList;
            if (presentationPart is null || slideIdList is null)
            {
                throw new InvalidOperationException("PPTX slide list was not found.");
            }

            var slides = new List<DocumentTextSegment>();
            var slideNumber = 1;
            foreach (var slideId in slideIdList.Elements<PresentationSlideId>())
            {
                cancellationToken.ThrowIfCancellationRequested();
                var relationshipId = slideId.RelationshipId?.Value;
                if (string.IsNullOrWhiteSpace(relationshipId))
                {
                    continue;
                }

                var slidePart = (SlidePart)presentationPart.GetPartById(relationshipId);
                var slideText = ExtractSlideText(slidePart).Trim();
                if (slideText.Length > 0)
                {
                    slides.Add(new DocumentTextSegment(slideText, slideNumber, $"Slide {slideNumber}", "PptxSlide"));
                }

                slideNumber++;
            }

            return Task.FromResult(ExtractedText.FromSegments(slides));
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
            throw new InvalidOperationException($"Failed to extract text from PPTX document: {exception.Message}", exception);
        }
    }

    private static string ExtractSlideText(SlidePart slidePart)
    {
        var textBlocks = new List<string>();
        if (slidePart.Slide is not null)
        {
            textBlocks.AddRange(slidePart.Slide
                .Descendants<A.Paragraph>()
                .Select(paragraph => string.Join(string.Empty, paragraph.Descendants<SlideText>().Select(text => text.Text)).Trim())
                .Where(text => text.Length > 0));
        }

        if (slidePart.NotesSlidePart?.NotesSlide is not null)
        {
            textBlocks.AddRange(slidePart.NotesSlidePart.NotesSlide
                .Descendants<A.Paragraph>()
                .Select(paragraph => string.Join(string.Empty, paragraph.Descendants<SlideText>().Select(text => text.Text)).Trim())
                .Where(text => text.Length > 0));
        }

        return string.Join("\n\n", textBlocks);
    }
}
