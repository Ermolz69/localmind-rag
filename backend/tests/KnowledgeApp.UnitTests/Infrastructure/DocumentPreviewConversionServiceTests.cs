using KnowledgeApp.Application.Abstractions;
using KnowledgeApp.Application.Common.Errors;
using KnowledgeApp.Application.Common.Results;
using KnowledgeApp.Domain.Enums;
using KnowledgeApp.Infrastructure.Options;
using KnowledgeApp.Infrastructure.Services.DocumentPreview;
using Microsoft.Extensions.Options;

namespace KnowledgeApp.UnitTests.Infrastructure;

public sealed class DocumentPreviewConversionServiceTests : IDisposable
{
    private readonly string root = Path.Combine(
        Path.GetTempPath(),
        $"localmind-preview-tests-{Guid.NewGuid():N}");

    [Fact]
    public async Task GetOrCreatePdfPreviewAsync_Should_Create_Cache_Path_By_Document_And_Content_Hash()
    {
        Guid documentId = Guid.NewGuid();
        FakeConverterProcess converter = new();
        LibreOfficeDocumentPreviewConversionService service = CreateService(converter);
        string sourcePath = CreateSourceFile("document.docx");

        Result<DocumentPreviewConversionResult> result =
            await service.GetOrCreatePdfPreviewAsync(CreateRequest(documentId, sourcePath, "abc123"));

        Assert.True(result.IsSuccess);
        Assert.Equal(
            Path.Combine(Paths.PreviewDirectory, documentId.ToString(), "abc123", "preview.pdf"),
            result.Value!.PreviewFilePath);
        Assert.True(File.Exists(result.Value.PreviewFilePath));
        Assert.Equal(1, converter.CallCount);
    }

    [Fact]
    public async Task GetOrCreatePdfPreviewAsync_Should_Use_Current_Cache_Without_Conversion()
    {
        Guid documentId = Guid.NewGuid();
        string sourcePath = CreateSourceFile("document.docx");
        string cachedPath = Path.Combine(
            Paths.PreviewDirectory,
            documentId.ToString(),
            "currenthash",
            "preview.pdf");

        Directory.CreateDirectory(Path.GetDirectoryName(cachedPath)!);
        await File.WriteAllBytesAsync(cachedPath, MinimalPdfBytes());

        FakeConverterProcess converter = new();
        LibreOfficeDocumentPreviewConversionService service = CreateService(converter);

        Result<DocumentPreviewConversionResult> result =
            await service.GetOrCreatePdfPreviewAsync(CreateRequest(documentId, sourcePath, "currenthash"));

        Assert.True(result.IsSuccess);
        Assert.Equal(cachedPath, result.Value!.PreviewFilePath);
        Assert.Equal(0, converter.CallCount);
    }

    [Fact]
    public async Task GetOrCreatePdfPreviewAsync_Should_Delete_Stale_Cache_After_Success()
    {
        Guid documentId = Guid.NewGuid();
        string sourcePath = CreateSourceFile("document.docx");
        string staleDirectory = Path.Combine(Paths.PreviewDirectory, documentId.ToString(), "oldhash");

        Directory.CreateDirectory(staleDirectory);
        await File.WriteAllTextAsync(Path.Combine(staleDirectory, "preview.pdf"), "stale");

        FakeConverterProcess converter = new();
        LibreOfficeDocumentPreviewConversionService service = CreateService(converter);

        Result<DocumentPreviewConversionResult> result =
            await service.GetOrCreatePdfPreviewAsync(CreateRequest(documentId, sourcePath, "newhash"));

        Assert.True(result.IsSuccess);
        Assert.True(File.Exists(result.Value!.PreviewFilePath));
        Assert.False(Directory.Exists(staleDirectory));
    }

    [Fact]
    public async Task GetOrCreatePdfPreviewAsync_Should_Map_Missing_Converter_To_Controlled_Error()
    {
        FakeConverterProcess converter = new(ApplicationErrors.ExternalDependency(
            ErrorCodes.Documents.PreviewConverterUnavailable,
            ErrorMessages.Documents.PreviewConverterUnavailable));
        LibreOfficeDocumentPreviewConversionService service = CreateService(converter);
        string sourcePath = CreateSourceFile("document.docx");

        Result<DocumentPreviewConversionResult> result =
            await service.GetOrCreatePdfPreviewAsync(CreateRequest(Guid.NewGuid(), sourcePath, "hash"));

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorCodes.Documents.PreviewConverterUnavailable, result.Error!.Code);
    }

    [Fact]
    public async Task GetOrCreatePdfPreviewAsync_Should_Map_Timeout_To_Controlled_Error()
    {
        FakeConverterProcess converter = new(ApplicationErrors.ExternalDependency(
            ErrorCodes.Documents.PreviewConversionTimeout,
            ErrorMessages.Documents.PreviewConversionTimeout));
        LibreOfficeDocumentPreviewConversionService service = CreateService(converter);
        string sourcePath = CreateSourceFile("document.docx");

        Result<DocumentPreviewConversionResult> result =
            await service.GetOrCreatePdfPreviewAsync(CreateRequest(Guid.NewGuid(), sourcePath, "hash"));

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorCodes.Documents.PreviewConversionTimeout, result.Error!.Code);
    }

    public void Dispose()
    {
        if (Directory.Exists(root))
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private TestPathProvider Paths => new(root);

    private LibreOfficeDocumentPreviewConversionService CreateService(FakeConverterProcess converter)
    {
        return new LibreOfficeDocumentPreviewConversionService(
            Paths,
            converter,
            Options.Create(new DocumentPreviewOptions()));
    }

    private string CreateSourceFile(string fileName)
    {
        string sourceDirectory = Path.Combine(root, "source");
        Directory.CreateDirectory(sourceDirectory);
        string sourcePath = Path.Combine(sourceDirectory, fileName);
        File.WriteAllText(sourcePath, "source content");
        return sourcePath;
    }

    private static DocumentPreviewConversionRequest CreateRequest(
        Guid documentId,
        string sourcePath,
        string contentHash)
    {
        return new DocumentPreviewConversionRequest(
            documentId,
            sourcePath,
            Path.GetFileName(sourcePath),
            contentHash,
            FileType.Docx);
    }

    private static byte[] MinimalPdfBytes()
    {
        return "%PDF-1.4\n1 0 obj\n<<>>\nendobj\ntrailer\n<<>>\n%%EOF"u8.ToArray();
    }

    private sealed class FakeConverterProcess(ApplicationError? error = null) : IDocumentPreviewConverterProcess
    {
        public int CallCount { get; private set; }

        public async Task<Result<DocumentPreviewProcessResult>> ConvertToPdfAsync(
            string sourcePath,
            string outputDirectory,
            TimeSpan timeout,
            CancellationToken cancellationToken = default)
        {
            CallCount++;

            if (error is not null)
            {
                return Result<DocumentPreviewProcessResult>.Failure(error);
            }

            Directory.CreateDirectory(outputDirectory);
            string pdfPath = Path.Combine(outputDirectory, "converted.pdf");
            await File.WriteAllBytesAsync(pdfPath, MinimalPdfBytes(), cancellationToken);

            return Result<DocumentPreviewProcessResult>.Success(new DocumentPreviewProcessResult(pdfPath));
        }
    }

    private sealed class TestPathProvider(string root) : IAppPathProvider
    {
        public string AppRootDirectory => root;
        public string DataDirectory => Path.Combine(root, "data");
        public string DatabasePath => Path.Combine(DataDirectory, "knowledge-app.db");
        public string FilesDirectory => Path.Combine(root, "files");
        public string PreviewDirectory => Path.Combine(root, "previews");
        public string IndexDirectory => Path.Combine(root, "indexes");
        public string LogsDirectory => Path.Combine(root, "logs");
    }
}
