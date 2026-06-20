using System.IO.Compression;
using System.Net;
using System.Text;
using System.Xml.Linq;
using KnowledgeApp.Domain.Enums;

namespace KnowledgeApp.Application.Documents;

internal static class OfficeDocumentHtmlConverter
{
    private static readonly XNamespace W =
        "http://schemas.openxmlformats.org/wordprocessingml/2006/main";

    private static readonly XNamespace A =
        "http://schemas.openxmlformats.org/drawingml/2006/main";

    private static readonly XNamespace Pml =
        "http://schemas.openxmlformats.org/presentationml/2006/main";

    private const string PageStyle =
        "box-sizing:border-box;min-height:100vh;background:#131313;" +
        "color:#e2e2e2;font-family:-apple-system,BlinkMacSystemFont,'Segoe UI',sans-serif;" +
        "font-size:14px;line-height:1.75;padding:28px 32px;";

    public static string? TryConvertToHtml(string filePath, FileType fileType)
    {
        try
        {
            return fileType switch
            {
                FileType.Docx => ConvertDocxToHtml(filePath),
                FileType.Pptx => ConvertPptxToHtml(filePath),
                _ => null,
            };
        }
        catch
        {
            return null;
        }
    }

    // ── DOCX ────────────────────────────────────────────────────────────────

    private static string ConvertDocxToHtml(string filePath)
    {
        using ZipArchive archive = ZipFile.OpenRead(filePath);
        ZipArchiveEntry? entry = archive.GetEntry("word/document.xml");
        if (entry is null) return Wrap("<p>No content found.</p>");

        using Stream stream = entry.Open();
        XDocument doc = XDocument.Load(stream);

        XElement? body = doc.Descendants(W + "body").FirstOrDefault();
        if (body is null) return Wrap("<p>No content found.</p>");

        var sb = new StringBuilder();
        bool inList = false;

        foreach (XElement child in body.Elements())
        {
            if (child.Name == W + "p")
            {
                bool isList = child.Descendants(W + "numPr").Any();

                if (isList && !inList)
                {
                    sb.Append("<ul style=\"margin:8px 0;padding-left:22px;\">");
                    inList = true;
                }
                else if (!isList && inList)
                {
                    sb.Append("</ul>");
                    inList = false;
                }

                AppendDocxParagraph(sb, child, inList);
            }
            else if (child.Name == W + "tbl")
            {
                if (inList) { sb.Append("</ul>"); inList = false; }
                AppendDocxTable(sb, child);
            }
        }

        if (inList) sb.Append("</ul>");

        return Wrap(sb.ToString());
    }

    private static void AppendDocxParagraph(StringBuilder sb, XElement para, bool inList)
    {
        string styleId = para.Element(W + "pPr")?.Element(W + "pStyle")?
            .Attribute(W + "val")?.Value ?? string.Empty;

        string content = BuildDocxRunContent(para);

        if (string.IsNullOrWhiteSpace(content))
        {
            if (!inList) sb.Append("<p style=\"margin:0;height:10px\"></p>");
            return;
        }

        if (inList)
        {
            sb.Append($"<li style=\"margin:3px 0\">{content}</li>");
            return;
        }

        (string tag, string style) = styleId.ToLowerInvariant() switch
        {
            "heading1" or "title" =>
                ("h1", "font-size:1.75em;font-weight:700;margin:22px 0 8px;color:#fff;border-bottom:1px solid #2d2d2d;padding-bottom:8px"),
            "heading2" or "subtitle" =>
                ("h2", "font-size:1.35em;font-weight:600;margin:18px 0 6px;color:#f0f0f0"),
            "heading3" =>
                ("h3", "font-size:1.12em;font-weight:600;margin:14px 0 4px;color:#e8e8e8"),
            "heading4" or "heading5" or "heading6" =>
                ("h4", "font-size:1em;font-weight:600;margin:10px 0 3px;color:#e0e0e0"),
            _ => ("p", "margin:5px 0"),
        };

        sb.Append($"<{tag} style=\"{style}\">{content}</{tag}>");
    }

    private static string BuildDocxRunContent(XElement para)
    {
        var sb = new StringBuilder();
        foreach (XElement child in para.Elements())
        {
            if (child.Name == W + "r")
                AppendDocxRun(sb, child);
            else if (child.Name == W + "hyperlink")
                foreach (XElement run in child.Elements(W + "r"))
                    AppendDocxRun(sb, run, isLink: true);
            else if (child.Name == W + "ins")
                foreach (XElement run in child.Elements(W + "r"))
                    AppendDocxRun(sb, run);
        }

        return sb.ToString();
    }

    private static void AppendDocxRun(StringBuilder sb, XElement run, bool isLink = false)
    {
        XElement? rPr = run.Element(W + "rPr");
        bool bold = IsToggleOn(rPr?.Element(W + "b"));
        bool italic = IsToggleOn(rPr?.Element(W + "i"));
        bool underline = rPr?.Element(W + "u") is { } u
            && u.Attribute(W + "val")?.Value is not ("none" or "0");

        var text = new StringBuilder();
        foreach (XElement part in run.Elements())
        {
            if (part.Name == W + "t") text.Append(WebUtility.HtmlEncode(part.Value));
            else if (part.Name == W + "br") text.Append("<br/>");
            else if (part.Name == W + "tab") text.Append("&#160;&#160;&#160;&#160;");
        }

        string result = text.ToString();
        if (string.IsNullOrEmpty(result)) return;

        if (isLink) result = $"<span style=\"text-decoration:underline;color:#7cb9e8\">{result}</span>";
        if (underline) result = $"<u>{result}</u>";
        if (italic) result = $"<em>{result}</em>";
        if (bold) result = $"<strong>{result}</strong>";

        sb.Append(result);
    }

    private static void AppendDocxTable(StringBuilder sb, XElement table)
    {
        sb.Append("<table style=\"width:100%;border-collapse:collapse;margin:14px 0;font-size:13px\">");
        bool firstRow = true;
        foreach (XElement row in table.Elements(W + "tr"))
        {
            sb.Append("<tr>");
            foreach (XElement cell in row.Elements(W + "tc"))
            {
                string tag = firstRow ? "th" : "td";
                string cellStyle = firstRow
                    ? "border:1px solid #333;padding:7px 10px;background:#1d1d1d;font-weight:600;color:#f0f0f0"
                    : "border:1px solid #333;padding:7px 10px;vertical-align:top";

                string cellContent = string.Concat(
                    cell.Elements(W + "p").Select(p => BuildDocxRunContent(p)));

                sb.Append($"<{tag} style=\"{cellStyle}\">{cellContent}</{tag}>");
            }
            sb.Append("</tr>");
            firstRow = false;
        }
        sb.Append("</table>");
    }

    private static bool IsToggleOn(XElement? element)
    {
        if (element is null) return false;
        string? val = element.Attribute(
            "http://schemas.openxmlformats.org/wordprocessingml/2006/main" + "val")?.Value
            ?? element.Attributes().FirstOrDefault(a => a.Name.LocalName == "val")?.Value;
        return val is null || (val != "false" && val != "0");
    }

    // ── PPTX ────────────────────────────────────────────────────────────────

    private static string ConvertPptxToHtml(string filePath)
    {
        using ZipArchive archive = ZipFile.OpenRead(filePath);

        List<ZipArchiveEntry> slides = archive.Entries
            .Where(e =>
                e.FullName.StartsWith("ppt/slides/slide", StringComparison.OrdinalIgnoreCase) &&
                e.FullName.EndsWith(".xml", StringComparison.OrdinalIgnoreCase) &&
                !e.FullName.Contains("/_rels/", StringComparison.OrdinalIgnoreCase))
            .OrderBy(e => e.FullName, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (slides.Count == 0)
            return Wrap("<p style=\"color:#888\">No slides found.</p>");

        var sb = new StringBuilder();
        int slideNumber = 1;

        foreach (ZipArchiveEntry slideEntry in slides)
        {
            using Stream stream = slideEntry.Open();
            XDocument doc = XDocument.Load(stream);
            AppendSlide(sb, doc, slideNumber++);
        }

        return Wrap(sb.ToString());
    }

    private static void AppendSlide(StringBuilder sb, XDocument doc, int slideNumber)
    {
        List<XElement> shapes = doc.Descendants(Pml + "sp").ToList();
        if (shapes.Count == 0) return;

        // Identify title shape
        XElement? titleShape = shapes.FirstOrDefault(s =>
        {
            string? phType = s.Descendants(Pml + "ph")
                .FirstOrDefault()?.Attribute("type")?.Value;
            return phType is "title" or "ctrTitle";
        }) ?? shapes.FirstOrDefault();

        string? titleText = GetShapeText(titleShape);

        bool hasContent = !string.IsNullOrWhiteSpace(titleText) ||
            shapes.Any(s => s != titleShape && !string.IsNullOrWhiteSpace(GetShapeText(s)));

        if (!hasContent) return;

        sb.Append("<div style=\"border:1px solid #2a2a2a;border-radius:10px;padding:22px 26px;margin-bottom:22px;background:#1a1a1a\">");
        sb.Append($"<div style=\"font-size:11px;color:#555;text-transform:uppercase;letter-spacing:.06em;margin-bottom:12px\">Slide {slideNumber}</div>");

        if (!string.IsNullOrWhiteSpace(titleText))
        {
            sb.Append($"<h2 style=\"font-size:1.3em;font-weight:700;margin:0 0 14px;color:#fff;border-bottom:1px solid #2d2d2d;padding-bottom:10px\">" +
                      $"{WebUtility.HtmlEncode(titleText)}</h2>");
        }

        foreach (XElement shape in shapes)
        {
            if (shape == titleShape) continue;

            string? phType = shape.Descendants(Pml + "ph")
                .FirstOrDefault()?.Attribute("type")?.Value;

            if (phType is "dt" or "ftr" or "sldNum") continue;

            AppendSlideShapeContent(sb, shape);
        }

        sb.Append("</div>");
    }

    private static void AppendSlideShapeContent(StringBuilder sb, XElement shape)
    {
        List<XElement> paragraphs = shape.Descendants(A + "p").ToList();
        if (paragraphs.Count == 0) return;

        bool inList = false;

        foreach (XElement para in paragraphs)
        {
            string text = BuildPptxParaHtml(para);
            if (string.IsNullOrWhiteSpace(text)) continue;

            bool isBullet = para.Descendants(A + "buChar").Any() ||
                            para.Descendants(A + "buAutoNum").Any() ||
                            (para.Element(A + "pPr")?.Attribute("marL") != null &&
                             !para.Descendants(A + "buNone").Any());

            if (isBullet && !inList)
            {
                sb.Append("<ul style=\"margin:6px 0;padding-left:20px;color:#d0d0d0\">");
                inList = true;
            }
            else if (!isBullet && inList)
            {
                sb.Append("</ul>");
                inList = false;
            }

            if (isBullet)
                sb.Append($"<li style=\"margin:3px 0\">{text}</li>");
            else
                sb.Append($"<p style=\"margin:4px 0;color:#d0d0d0\">{text}</p>");
        }

        if (inList) sb.Append("</ul>");
    }

    private static string? GetShapeText(XElement? shape)
    {
        if (shape is null) return null;
        string text = string.Concat(shape.Descendants(A + "t").Select(t => t.Value));
        return string.IsNullOrWhiteSpace(text) ? null : text;
    }

    private static string BuildPptxParaHtml(XElement para)
    {
        var sb = new StringBuilder();
        foreach (XElement run in para.Elements(A + "r"))
        {
            XElement? rPr = run.Element(A + "rPr");
            bool bold = rPr?.Attribute("b")?.Value == "1";
            bool italic = rPr?.Attribute("i")?.Value == "1";

            string text = string.Concat(run.Elements(A + "t").Select(t => t.Value));
            if (string.IsNullOrEmpty(text)) continue;

            string escaped = WebUtility.HtmlEncode(text);
            if (italic) escaped = $"<em>{escaped}</em>";
            if (bold) escaped = $"<strong>{escaped}</strong>";
            sb.Append(escaped);
        }

        return sb.ToString();
    }

    private static string Wrap(string content) =>
        $"<div style=\"{PageStyle}\">{content}</div>";
}
