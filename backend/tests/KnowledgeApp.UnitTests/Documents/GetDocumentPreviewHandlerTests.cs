using KnowledgeApp.Application.Documents;
using KnowledgeApp.Contracts.Documents;
using KnowledgeApp.Domain.Entities;
using KnowledgeApp.Domain.Enums;
using KnowledgeApp.Infrastructure.Services.Persistence;
using KnowledgeApp.UnitTests.TestSupport;
using KnowledgeApp.UnitTests.TestSupport.Builders;
using KnowledgeApp.UnitTests.TestSupport.Fakes;
using KnowledgeApp.UnitTests.TestSupport.Fixtures;

namespace KnowledgeApp.UnitTests.Documents;

public sealed class GetDocumentPreviewHandlerTests
{
    [Fact]
    public async Task HandleAsync_Should_Return_Text_Preview_For_PlainText_Document()
    {
        await using ApplicationTestDatabase database = await ApplicationTestDatabase.CreateAsync();
        await using ManagedFileTestStorage storage = ManagedFileTestStorage.Create();
        DocumentWithFileTestData testData = await DocumentWithFileTestData.CreateAsync(
            database, storage, PreviewFixtures.PlainTextFileName, FileType.PlainText, PreviewFixtures.PlainText);

        var result = await CreateHandler(database, storage)
            .HandleAsync(new GetDocumentPreviewQuery(testData.DocumentId));

        Assert.True(result.IsSuccess);
        Assert.Equal(DocumentPreviewKind.Text, result.Value!.PreviewKind);
        Assert.Equal(PreviewFixtures.PlainText, result.Value.TextContent);
        Assert.Null(result.Value.PreviewUrl);
        Assert.Null(result.Value.ErrorCode);
    }

    [Fact]
    public async Task HandleAsync_Should_Return_Markdown_Preview_For_Markdown_Document()
    {
        await using ApplicationTestDatabase database = await ApplicationTestDatabase.CreateAsync();
        await using ManagedFileTestStorage storage = ManagedFileTestStorage.Create();
        DocumentWithFileTestData testData = await DocumentWithFileTestData.CreateAsync(
            database, storage, PreviewFixtures.MarkdownFileName, FileType.Markdown, PreviewFixtures.Markdown);

        var result = await CreateHandler(database, storage)
            .HandleAsync(new GetDocumentPreviewQuery(testData.DocumentId));

        Assert.True(result.IsSuccess);
        Assert.Equal(DocumentPreviewKind.Markdown, result.Value!.PreviewKind);
        Assert.Equal(PreviewFixtures.Markdown, result.Value.TextContent);
        Assert.Null(result.Value.PreviewUrl);
    }

    [Fact]
    public async Task HandleAsync_Should_Return_Html_Preview_For_Html_Document()
    {
        await using ApplicationTestDatabase database = await ApplicationTestDatabase.CreateAsync();
        await using ManagedFileTestStorage storage = ManagedFileTestStorage.Create();
        DocumentWithFileTestData testData = await DocumentWithFileTestData.CreateAsync(
            database, storage, PreviewFixtures.HtmlFileName, FileType.Html, PreviewFixtures.Html);

        var result = await CreateHandler(database, storage)
            .HandleAsync(new GetDocumentPreviewQuery(testData.DocumentId));

        Assert.True(result.IsSuccess);
        Assert.Equal(DocumentPreviewKind.Html, result.Value!.PreviewKind);
        Assert.Equal(PreviewFixtures.Html, result.Value.TextContent);
        Assert.Null(result.Value.PreviewUrl);
    }

    [Fact]
    public async Task HandleAsync_Should_Return_Pdf_Preview_Url_For_Pdf_Document()
    {
        await using ApplicationTestDatabase database = await ApplicationTestDatabase.CreateAsync();
        await using ManagedFileTestStorage storage = ManagedFileTestStorage.Create();
        DocumentWithFileTestData testData = await DocumentWithFileTestData.CreateAsync(
            database, storage, PreviewFixtures.PdfFileName, FileType.Pdf, PreviewFixtures.MinimalPdfBytes());

        var result = await CreateHandler(database, storage)
            .HandleAsync(new GetDocumentPreviewQuery(testData.DocumentId));

        Assert.True(result.IsSuccess);
        Assert.Equal(DocumentPreviewKind.Pdf, result.Value!.PreviewKind);
        Assert.Equal($"/api/v1/documents/{testData.DocumentId}/preview/file", result.Value.PreviewUrl);
        Assert.Null(result.Value.TextContent);
        Assert.Null(result.Value.ErrorCode);
    }

    [Fact]
    public async Task HandleAsync_Should_Return_Error_When_File_Path_Is_Outside_Managed_Storage()
    {
        await using ApplicationTestDatabase database = await ApplicationTestDatabase.CreateAsync();
        await using ManagedFileTestStorage storage = ManagedFileTestStorage.Create();

        string externalPath = Path.Combine(Path.GetTempPath(), $"ext-preview-{Guid.NewGuid():N}.txt");

        try
        {
            await File.WriteAllTextAsync(externalPath, "external content");

            Document doc = new() { Name = "external.txt", Status = DocumentStatus.Uploaded };
            database.Context.Documents.Add(doc);
            database.Context.DocumentFiles.Add(new DocumentFile
            {
                DocumentId = doc.Id,
                FileName = "external.txt",
                FileType = FileType.PlainText,
                LocalPath = externalPath,
                SizeBytes = 0,
            });
            await database.Context.SaveChangesAsync();

            var result = await CreateHandler(database, storage)
                .HandleAsync(new GetDocumentPreviewQuery(doc.Id));

            Assert.True(result.IsSuccess);
            Assert.Equal(DocumentPreviewKind.Error, result.Value!.PreviewKind);
            Assert.Equal("DOCUMENT_PREVIEW_FILE_MISSING", result.Value.ErrorCode);
            Assert.Null(result.Value.TextContent);
            Assert.Null(result.Value.PreviewUrl);
        }
        finally
        {
            if (File.Exists(externalPath)) File.Delete(externalPath);
        }
    }

    [Fact]
    public async Task HandleAsync_Should_Return_Error_When_Inline_Text_Exceeds_Size_Limit()
    {
        await using ApplicationTestDatabase database = await ApplicationTestDatabase.CreateAsync();
        await using ManagedFileTestStorage storage = ManagedFileTestStorage.Create();
        DocumentWithFileTestData testData = await DocumentWithFileTestData.CreateAsync(
            database, storage, PreviewFixtures.PlainTextFileName, FileType.PlainText,
            PreviewFixtures.LargeTextContent());

        var result = await CreateHandler(database, storage)
            .HandleAsync(new GetDocumentPreviewQuery(testData.DocumentId));

        Assert.True(result.IsSuccess);
        Assert.Equal(DocumentPreviewKind.Error, result.Value!.PreviewKind);
        Assert.Equal("DOCUMENT_PREVIEW_UNAVAILABLE", result.Value.ErrorCode);
        Assert.Contains("256 KB", result.Value.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task HandleAsync_Should_Return_Failure_When_Document_Does_Not_Exist()
    {
        await using ApplicationTestDatabase database = await ApplicationTestDatabase.CreateAsync();
        await using ManagedFileTestStorage storage = ManagedFileTestStorage.Create();

        var result = await CreateHandler(database, storage)
            .HandleAsync(new GetDocumentPreviewQuery(Guid.NewGuid()));

        Assert.False(result.IsSuccess);
        Assert.Equal("DOCUMENT_NOT_FOUND", result.Error!.Code);
    }

    private static GetDocumentPreviewHandler CreateHandler(
        ApplicationTestDatabase database,
        ManagedFileTestStorage storage)
        => new(
            new DocumentRepository(database.Context),
            new StubAppPathProvider(storage),
            new FakeDocumentPreviewConversionService());
}
