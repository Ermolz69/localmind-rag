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

public sealed class DocumentTextExtractorFactory(
    RawTextExtractor rawTextExtractor,
    HtmlTextExtractor htmlTextExtractor,
    PdfTextExtractor pdfTextExtractor,
    DocxTextExtractor docxTextExtractor,
    PptxTextExtractor pptxTextExtractor) : IDocumentTextExtractorFactory
{
    public IDocumentTextExtractor GetExtractor(FileType fileType, string extension, string? mimeType)
    {
        string? normalizedExtension = extension.ToLowerInvariant();
        return fileType switch
        {
            FileType.Pdf => pdfTextExtractor,
            FileType.Docx => docxTextExtractor,
            FileType.Pptx => pptxTextExtractor,
            FileType.PlainText => rawTextExtractor,
            FileType.Markdown => rawTextExtractor,
            FileType.Html => htmlTextExtractor,
            FileType.Unknown when normalizedExtension is ".pdf" => pdfTextExtractor,
            FileType.Unknown when normalizedExtension is ".docx" => docxTextExtractor,
            FileType.Unknown when normalizedExtension is ".pptx" => pptxTextExtractor,
            FileType.Unknown when normalizedExtension is ".txt" => rawTextExtractor,
            FileType.Unknown when normalizedExtension is ".md" or ".markdown" => rawTextExtractor,
            FileType.Unknown when normalizedExtension is ".html" or ".htm" => htmlTextExtractor,
            _ => new UnsupportedDocumentTextExtractor(fileType),
        };
    }
}
