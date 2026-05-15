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

public sealed class PdfTextExtractor(
    IOcrEngine ocrEngine,
    Microsoft.Extensions.Options.IOptions<OcrOptions> ocrOptions) : IDocumentTextExtractor
{
    public async Task<DocumentTextExtractionResult> ExtractAsync(
        string filePath,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            FileSignatureValidator.EnsurePdf(filePath);

            using var document = PdfDocument.Open(filePath);
            var segments = new List<DocumentTextSegment>();

            foreach (var page in document.GetPages())
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (!string.IsNullOrWhiteSpace(page.Text))
                {
                    segments.Add(new DocumentTextSegment(
                        page.Text.Trim(),
                        page.Number,
                        $"Page {page.Number}",
                        "PdfPage"));
                }

                if (!ocrOptions.Value.Enabled)
                {
                    continue;
                }

                var imageNumber = 1;

                foreach (var image in page.GetImages())
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (image.WidthInSamples < ocrOptions.Value.MinimumImageWidth
                        || image.HeightInSamples < ocrOptions.Value.MinimumImageHeight)
                    {
                        imageNumber++;
                        continue;
                    }

                    var temporaryImagePath = TryWritePdfImageToTemporaryFile(image, page.Number, imageNumber);

                    if (temporaryImagePath is null)
                    {
                        imageNumber++;
                        continue;
                    }

                    try
                    {
                        var ocrResult = await ocrEngine.ExtractAsync(temporaryImagePath, cancellationToken);

                        if (!string.IsNullOrWhiteSpace(ocrResult.Text))
                        {
                            var languageSuffix = string.IsNullOrWhiteSpace(ocrResult.DetectedLanguage)
                                ? string.Empty
                                : $" ({ocrResult.DetectedLanguage})";

                            segments.Add(new DocumentTextSegment(
                                ocrResult.Text,
                                page.Number,
                                $"Page {page.Number} image {imageNumber} OCR{languageSuffix}",
                                "PdfImageOcr"));
                        }
                    }
                    finally
                    {
                        TryDeleteFile(temporaryImagePath);
                    }

                    imageNumber++;
                }
            }

            return ExtractedText.FromSegments(segments);
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
            throw new InvalidOperationException($"Failed to extract text from PDF document: {exception.Message}", exception);
        }
    }

    private static string? TryWritePdfImageToTemporaryFile(
        UglyToad.PdfPig.Content.IPdfImage image,
        int pageNumber,
        int imageNumber)
    {
        var temporaryDirectory = Path.Combine(Path.GetTempPath(), "localmind-rag-ocr");
        Directory.CreateDirectory(temporaryDirectory);

        if (image.TryGetPng(out var pngBytes))
        {
            var pngPath = Path.Combine(
                temporaryDirectory,
                $"pdf-page-{pageNumber}-image-{imageNumber}-{Guid.NewGuid():N}.png");

            File.WriteAllBytes(pngPath, pngBytes);
            return pngPath;
        }

        var rawBytes = image.RawBytes.ToArray();

        if (IsJpeg(rawBytes))
        {
            var jpgPath = Path.Combine(
                temporaryDirectory,
                $"pdf-page-{pageNumber}-image-{imageNumber}-{Guid.NewGuid():N}.jpg");

            File.WriteAllBytes(jpgPath, rawBytes);
            return jpgPath;
        }

        return null;
    }

    private static bool IsJpeg(byte[] bytes)
    {
        return bytes.Length >= 4
            && bytes[0] == 0xFF
            && bytes[1] == 0xD8
            && bytes[^2] == 0xFF
            && bytes[^1] == 0xD9;
    }

    private static void TryDeleteFile(string filePath)
    {
        try
        {
            File.Delete(filePath);
        }
        catch
        {
            // Temporary OCR files are best-effort cleanup.
        }
    }
}
