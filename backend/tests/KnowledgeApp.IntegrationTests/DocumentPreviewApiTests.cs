using System.Net;
using System.Text;
using KnowledgeApp.Contracts.Documents;
using KnowledgeApp.IntegrationTests.TestSupport;

namespace KnowledgeApp.IntegrationTests;

public sealed class DocumentPreviewApiTests : IClassFixture<LocalApiTestFactory>
{
    private readonly LocalApiTestFactory factory;

    public DocumentPreviewApiTests(LocalApiTestFactory factory)
    {
        this.factory = factory;
    }

    [Fact]
    public async Task GetDocumentPreview_Should_Return_Inline_Markdown_For_Markdown_Document()
    {
        using HttpClient client = factory.CreateClient();

        string content = "# Heading\n\nMarkdown preview **test**.\n";
        UploadDocumentResponse upload = await ApiScenarioHelpers.UploadTextDocumentAsync(
            client,
            content,
            $"preview-{Guid.NewGuid():N}.md");

        DocumentPreviewResponse? preview =
            await client.GetApiDataAsync<DocumentPreviewResponse>(
                $"/api/v1/documents/{upload.DocumentId}/preview");

        Assert.NotNull(preview);
        Assert.Equal(upload.DocumentId, preview.DocumentId);
        Assert.Equal(DocumentPreviewKind.Markdown, preview.PreviewKind);
        Assert.Equal(content, preview.TextContent);
        Assert.Null(preview.PreviewUrl);
        Assert.Null(preview.ErrorCode);
    }

    [Fact]
    public async Task GetDocumentPreview_Should_Return_Inline_Html_For_Html_Document()
    {
        using HttpClient client = factory.CreateClient();

        string content = "<html><body><p>HTML preview test</p></body></html>";
        UploadDocumentResponse upload = await ApiScenarioHelpers.UploadTextDocumentAsync(
            client,
            content,
            $"preview-{Guid.NewGuid():N}.html");

        DocumentPreviewResponse? preview =
            await client.GetApiDataAsync<DocumentPreviewResponse>(
                $"/api/v1/documents/{upload.DocumentId}/preview");

        Assert.NotNull(preview);
        Assert.Equal(upload.DocumentId, preview.DocumentId);
        Assert.Equal(DocumentPreviewKind.Html, preview.PreviewKind);
        Assert.Equal(content, preview.TextContent);
        Assert.Null(preview.PreviewUrl);
        Assert.Null(preview.ErrorCode);
    }

    [Fact]
    public async Task GetDocumentPreview_Should_Return_Error_When_Text_File_Exceeds_Inline_Size_Limit()
    {
        using HttpClient client = factory.CreateClient();

        string largeContent = new('A', 270 * 1024);
        UploadDocumentResponse upload = await ApiScenarioHelpers.UploadTextDocumentAsync(
            client,
            largeContent,
            $"preview-large-{Guid.NewGuid():N}.txt");

        DocumentPreviewResponse? preview =
            await client.GetApiDataAsync<DocumentPreviewResponse>(
                $"/api/v1/documents/{upload.DocumentId}/preview");

        Assert.NotNull(preview);
        Assert.Equal(DocumentPreviewKind.Error, preview.PreviewKind);
        Assert.Equal("DOCUMENT_PREVIEW_UNAVAILABLE", preview.ErrorCode);
        Assert.Contains("256 KB", preview.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Null(preview.TextContent);
        Assert.Null(preview.PreviewUrl);
    }

    [Fact]
    public async Task GetDocumentPreview_Response_Should_Not_Contain_Local_File_Path()
    {
        using HttpClient client = factory.CreateClient();

        UploadDocumentResponse upload = await ApiScenarioHelpers.UploadTextDocumentAsync(
            client,
            "preview path safety check",
            $"preview-{Guid.NewGuid():N}.txt");

        using HttpResponseMessage response =
            await client.GetAsync($"/api/v1/documents/{upload.DocumentId}/preview");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        string body = await response.Content.ReadAsStringAsync();

        ApiScenarioHelpers.AssertNoLocalPathInResponseBody(body);
    }

    [Fact]
    public async Task GetDocumentPreview_Should_Return_Correct_Content_Types()
    {
        using HttpClient client = factory.CreateClient();

        var cases = new[]
        {
            (fileName: $"ct-{Guid.NewGuid():N}.txt", content: "plain", expectedKind: DocumentPreviewKind.Text, expectedContentType: "text/plain; charset=utf-8"),
            (fileName: $"ct-{Guid.NewGuid():N}.md", content: "# md", expectedKind: DocumentPreviewKind.Markdown, expectedContentType: "text/markdown; charset=utf-8"),
            (fileName: $"ct-{Guid.NewGuid():N}.html", content: "<p>html</p>", expectedKind: DocumentPreviewKind.Html, expectedContentType: "text/html; charset=utf-8"),
        };

        foreach (var (fileName, content, expectedKind, expectedContentType) in cases)
        {
            UploadDocumentResponse upload =
                await ApiScenarioHelpers.UploadTextDocumentAsync(client, content, fileName);

            DocumentPreviewResponse? preview =
                await client.GetApiDataAsync<DocumentPreviewResponse>(
                    $"/api/v1/documents/{upload.DocumentId}/preview");

            Assert.NotNull(preview);
            Assert.Equal(expectedKind, preview.PreviewKind);
            Assert.Equal(expectedContentType, preview.ContentType);
        }
    }
}
