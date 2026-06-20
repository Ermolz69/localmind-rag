using KnowledgeApp.Application.Abstractions;
using KnowledgeApp.Application.Common.Errors;
using KnowledgeApp.Application.Common.Results;
using KnowledgeApp.Domain.Enums;
using KnowledgeApp.Infrastructure.Options;
using Microsoft.Extensions.Options;

namespace KnowledgeApp.Infrastructure.Services.DocumentPreview;

public sealed class LibreOfficeDocumentPreviewConversionService(
    IAppPathProvider paths,
    IDocumentPreviewConverterProcess converterProcess,
    IOptions<DocumentPreviewOptions> options) : IDocumentPreviewConversionService
{
    private const string PreviewFileName = "preview.pdf";
    private const string PdfContentType = "application/pdf";

    private readonly DocumentPreviewOptions previewOptions = options.Value;

    public async Task<Result<DocumentPreviewConversionResult>> GetOrCreatePdfPreviewAsync(
        DocumentPreviewConversionRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.FileType is not (FileType.Docx or FileType.Pptx))
        {
            return Result<DocumentPreviewConversionResult>.Failure(ApplicationErrors.UnsupportedMedia(
                ErrorCodes.Documents.PreviewUnsupported,
                ErrorMessages.Documents.PreviewUnsupported));
        }

        string cacheDirectory = GetCacheDirectory(request.DocumentId, request.ContentHash);
        string previewPath = Path.Combine(cacheDirectory, PreviewFileName);

        if (File.Exists(previewPath))
        {
            return Result<DocumentPreviewConversionResult>.Success(CreateResult(previewPath));
        }

        string tempDirectory = Path.Combine(
            paths.PreviewDirectory,
            "_tmp",
            Guid.NewGuid().ToString("N"));

        try
        {
            Directory.CreateDirectory(tempDirectory);

            Result<DocumentPreviewProcessResult> conversionResult =
                await converterProcess.ConvertToPdfAsync(
                    request.SourcePath,
                    tempDirectory,
                    TimeSpan.FromSeconds(previewOptions.ConversionTimeoutSeconds),
                    cancellationToken);

            if (!conversionResult.IsSuccess)
            {
                return Result<DocumentPreviewConversionResult>.Failure(conversionResult.Error!);
            }

            string generatedPath = conversionResult.Value!.PdfPath;
            if (!File.Exists(generatedPath) || new FileInfo(generatedPath).Length == 0)
            {
                return Result<DocumentPreviewConversionResult>.Failure(ApplicationErrors.Unexpected(
                    ErrorCodes.Documents.PreviewConversionFailed,
                    ErrorMessages.Documents.PreviewConversionFailed));
            }

            Directory.CreateDirectory(cacheDirectory);
            string tempTarget = Path.Combine(
                cacheDirectory,
                $"{PreviewFileName}.{Guid.NewGuid():N}.tmp");

            File.Copy(generatedPath, tempTarget, overwrite: true);
            File.Move(tempTarget, previewPath, overwrite: true);
            DeleteStaleCacheDirectories(request.DocumentId, cacheDirectory);

            return Result<DocumentPreviewConversionResult>.Success(CreateResult(previewPath));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return Result<DocumentPreviewConversionResult>.Failure(ApplicationErrors.Unexpected(
                ErrorCodes.Documents.PreviewConversionFailed,
                ErrorMessages.Documents.PreviewConversionFailed));
        }
        finally
        {
            TryDeleteDirectory(tempDirectory);
        }
    }

    private string GetCacheDirectory(Guid documentId, string contentHash)
    {
        string safeHash = MakeSafePathSegment(contentHash);
        return Path.Combine(paths.PreviewDirectory, documentId.ToString(), safeHash);
    }

    private void DeleteStaleCacheDirectories(Guid documentId, string currentCacheDirectory)
    {
        string documentCacheDirectory = Path.Combine(paths.PreviewDirectory, documentId.ToString());
        if (!Directory.Exists(documentCacheDirectory))
        {
            return;
        }

        string fullCurrentCacheDirectory = Path.GetFullPath(currentCacheDirectory);

        foreach (string directory in Directory.EnumerateDirectories(documentCacheDirectory))
        {
            if (string.Equals(Path.GetFullPath(directory), fullCurrentCacheDirectory, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            TryDeleteDirectory(directory);
        }
    }

    private static void TryDeleteDirectory(string directory)
    {
        try
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
            }
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    private static string MakeSafePathSegment(string value)
    {
        char[] chars = value
            .Where(character => char.IsAsciiLetterOrDigit(character) || character is '-' or '_')
            .ToArray();

        return chars.Length == 0
            ? "unknown"
            : new string(chars);
    }

    private static DocumentPreviewConversionResult CreateResult(string previewPath)
    {
        return new DocumentPreviewConversionResult(
            previewPath,
            PreviewFileName,
            PdfContentType);
    }
}
