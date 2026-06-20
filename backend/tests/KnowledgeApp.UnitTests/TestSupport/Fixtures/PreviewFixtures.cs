using System.Globalization;
using System.Text;

namespace KnowledgeApp.UnitTests.TestSupport.Fixtures;

internal static class PreviewFixtures
{
    // ── Common file names ───────────────────────────────────────────────────

    public const string PlainTextFileName = "fixture.txt";
    public const string MarkdownFileName = "fixture.md";
    public const string HtmlFileName = "fixture.html";
    public const string PdfFileName = "fixture.pdf";
    public const string DocxFileName = "fixture.docx";
    public const string UnsupportedFileName = "fixture.xyz";

    // ── Text-based fixture content ──────────────────────────────────────────

    public const string PlainText =
        "This is a plain text preview fixture.\nLine two.\n";

    public const string Markdown =
        "# Test Document\n\nThis is a **markdown** preview fixture.\n\n- Item one\n- Item two\n";

    public const string Html =
        "<html><head><title>Test</title></head><body><p>HTML preview fixture.</p></body></html>";

    // ── Binary fixture content ──────────────────────────────────────────────

    public static byte[] MinimalPdfBytes() => BuildPdfBytes("Preview fixture.");

    // A byte sequence with no matching supported file type (zip magic bytes).
    public static byte[] UnsupportedBytes() => [0x50, 0x4B, 0x03, 0x04, 0x00, 0x00];

    // Large content that exceeds the 256 KB inline preview limit (handler cap).
    public static string LargeTextContent(int sizeBytes = 270 * 1024) => new('A', sizeBytes);

    // ── PDF creation ────────────────────────────────────────────────────────

    public static byte[] BuildPdfBytes(string text)
    {
        string escaped = text
            .Replace(@"\", @"\\", StringComparison.Ordinal)
            .Replace("(", @"\(", StringComparison.Ordinal)
            .Replace(")", @"\)", StringComparison.Ordinal);

        string stream = $"BT /F1 12 Tf 72 720 Td ({escaped}) Tj ET";

        string[] objects =
        [
            "<< /Type /Catalog /Pages 2 0 R >>",
            "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
            "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Resources << /Font << /F1 5 0 R >> >> /Contents 4 0 R >>",
            $"<< /Length {Encoding.ASCII.GetByteCount(stream)} >>\nstream\n{stream}\nendstream",
            "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>",
        ];

        StringBuilder sb = new("%PDF-1.4\n");
        List<int> offsets = [0];

        for (int i = 0; i < objects.Length; i++)
        {
            offsets.Add(Encoding.ASCII.GetByteCount(sb.ToString()));
            sb.Append(CultureInfo.InvariantCulture, $"{i + 1} 0 obj\n{objects[i]}\nendobj\n");
        }

        int xrefOffset = Encoding.ASCII.GetByteCount(sb.ToString());

        sb.Append(CultureInfo.InvariantCulture, $"xref\n0 {objects.Length + 1}\n");
        sb.Append("0000000000 65535 f \n");

        foreach (int offset in offsets.Skip(1))
            sb.Append(CultureInfo.InvariantCulture, $"{offset:D10} 00000 n \n");

        sb.Append(CultureInfo.InvariantCulture,
            $"trailer\n<< /Size {objects.Length + 1} /Root 1 0 R >>\nstartxref\n{xrefOffset}\n%%EOF");

        return Encoding.ASCII.GetBytes(sb.ToString());
    }
}
