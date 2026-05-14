using System.Globalization;
using System.Text;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using KnowledgeApp.Infrastructure.Services;
using A = DocumentFormat.OpenXml.Drawing;
using P = DocumentFormat.OpenXml.Presentation;
using W = DocumentFormat.OpenXml.Wordprocessing;

namespace KnowledgeApp.UnitTests.Ingestion;

public sealed class DocumentTextExtractorTests : IAsyncDisposable
{
    private readonly List<string> filesToDelete = [];

    [Fact]
    public async Task PdfTextExtractor_Should_Return_Text_By_Page()
    {
        var filePath = await WriteTempFileAsync("document.pdf", CreatePdfBytes("PDF page text."));
        var extractor = new PdfTextExtractor();

        var result = await extractor.ExtractAsync(filePath);

        var segment = Assert.Single(result.Segments);
        Assert.Contains("PDF page text.", segment.Text, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(1, segment.PageNumber);
        Assert.Equal("Page 1", segment.SectionTitle);
        Assert.Equal("PdfPage", segment.SourceKind);
    }

    [Fact]
    public async Task DocxTextExtractor_Should_Return_Paragraph_Text()
    {
        var filePath = await WriteTempFileAsync("document.docx", CreateDocxBytes("DOCX paragraph text."));
        var extractor = new DocxTextExtractor();

        var result = await extractor.ExtractAsync(filePath);

        var segment = Assert.Single(result.Segments);
        Assert.Equal("DOCX paragraph text.", segment.Text);
        Assert.Null(segment.PageNumber);
        Assert.Equal("DocxDocument", segment.SourceKind);
    }

    [Fact]
    public async Task PptxTextExtractor_Should_Return_Text_By_Slide()
    {
        var filePath = await WriteTempFileAsync("slides.pptx", CreatePptxBytes("PPTX slide text."));
        var extractor = new PptxTextExtractor();

        var result = await extractor.ExtractAsync(filePath);

        var segment = Assert.Single(result.Segments);
        Assert.Equal("PPTX slide text.", segment.Text);
        Assert.Equal(1, segment.PageNumber);
        Assert.Equal("Slide 1", segment.SectionTitle);
        Assert.Equal("PptxSlide", segment.SourceKind);
    }

    [Fact]
    public async Task PdfTextExtractor_Should_Reject_Invalid_Signature()
    {
        var filePath = await WriteTempFileAsync("document.pdf", Encoding.UTF8.GetBytes("not a pdf"));
        var extractor = new PdfTextExtractor();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => extractor.ExtractAsync(filePath));

        Assert.Contains("PDF", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var filePath in filesToDelete)
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }

        await Task.CompletedTask;
    }

    private async Task<string> WriteTempFileAsync(string fileName, byte[] content)
    {
        var filePath = Path.Combine(Path.GetTempPath(), $"localmind-extractor-{Guid.NewGuid():N}-{fileName}");
        await File.WriteAllBytesAsync(filePath, content);
        filesToDelete.Add(filePath);
        return filePath;
    }

    private static byte[] CreatePdfBytes(string text)
    {
        var escapedText = text.Replace(@"\", @"\\", StringComparison.Ordinal)
            .Replace("(", @"\(", StringComparison.Ordinal)
            .Replace(")", @"\)", StringComparison.Ordinal);
        var content = $"BT /F1 12 Tf 72 720 Td ({escapedText}) Tj ET";
        var objects = new[]
        {
            "<< /Type /Catalog /Pages 2 0 R >>",
            "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
            "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Resources << /Font << /F1 5 0 R >> >> /Contents 4 0 R >>",
            $"<< /Length {Encoding.ASCII.GetByteCount(content)} >>\nstream\n{content}\nendstream",
            "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>",
        };
        var builder = new StringBuilder("%PDF-1.4\n");
        var offsets = new List<int> { 0 };
        for (var index = 0; index < objects.Length; index++)
        {
            offsets.Add(Encoding.ASCII.GetByteCount(builder.ToString()));
            builder.Append(CultureInfo.InvariantCulture, $"{index + 1} 0 obj\n{objects[index]}\nendobj\n");
        }

        var xrefOffset = Encoding.ASCII.GetByteCount(builder.ToString());
        builder.Append(CultureInfo.InvariantCulture, $"xref\n0 {objects.Length + 1}\n");
        builder.Append("0000000000 65535 f \n");
        foreach (var offset in offsets.Skip(1))
        {
            builder.Append(CultureInfo.InvariantCulture, $"{offset:D10} 00000 n \n");
        }

        builder.Append(CultureInfo.InvariantCulture, $"trailer\n<< /Size {objects.Length + 1} /Root 1 0 R >>\nstartxref\n{xrefOffset}\n%%EOF");
        return Encoding.ASCII.GetBytes(builder.ToString());
    }

    private static byte[] CreateDocxBytes(string text)
    {
        using var stream = new MemoryStream();
        using (var document = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document, true))
        {
            var mainDocumentPart = document.AddMainDocumentPart();
            mainDocumentPart.Document = new W.Document(new W.Body(new W.Paragraph(new W.Run(new W.Text(text)))));
            mainDocumentPart.Document.Save();
        }

        return stream.ToArray();
    }

    private static byte[] CreatePptxBytes(string text)
    {
        using var stream = new MemoryStream();
        using (var document = PresentationDocument.Create(stream, PresentationDocumentType.Presentation, true))
        {
            var presentationPart = document.AddPresentationPart();
            presentationPart.Presentation = new P.Presentation();
            var slidePart = presentationPart.AddNewPart<SlidePart>("rId1");
            slidePart.Slide = new P.Slide(
                new P.CommonSlideData(
                    new P.ShapeTree(
                        new P.NonVisualGroupShapeProperties(
                            new P.NonVisualDrawingProperties { Id = 1U, Name = string.Empty },
                            new P.NonVisualGroupShapeDrawingProperties(),
                            new P.ApplicationNonVisualDrawingProperties()),
                        new P.GroupShapeProperties(new A.TransformGroup()),
                        new P.Shape(
                            new P.NonVisualShapeProperties(
                                new P.NonVisualDrawingProperties { Id = 2U, Name = "Text" },
                                new P.NonVisualShapeDrawingProperties(),
                                new P.ApplicationNonVisualDrawingProperties()),
                            new P.ShapeProperties(),
                            new P.TextBody(
                                new A.BodyProperties(),
                                new A.ListStyle(),
                                new A.Paragraph(new A.Run(new A.Text(text))))))));
            slidePart.Slide.Save();
            presentationPart.Presentation.AppendChild(new P.SlideIdList(new P.SlideId { Id = 256U, RelationshipId = "rId1" }));
            presentationPart.Presentation.Save();
        }

        return stream.ToArray();
    }
}
