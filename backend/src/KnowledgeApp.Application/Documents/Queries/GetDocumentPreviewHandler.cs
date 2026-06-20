using System.Text;
using KnowledgeApp.Application.Abstractions;
using KnowledgeApp.Application.Common.Errors;
using KnowledgeApp.Application.Common.Results;
using KnowledgeApp.Contracts.Documents;
using KnowledgeApp.Domain.Entities;
using KnowledgeApp.Domain.Enums;

namespace KnowledgeApp.Application.Documents;

public sealed class GetDocumentPreviewHandler(
    IDocumentRepository documentRepository,
    IAppPathProvider paths,
    IDocumentPreviewConversionService previewConversionService)
{
    private const int MaxInlineTextBytes = 256 * 1024;
    private static readonly Encoding StrictUtf8 = new UTF8Encoding(false, true);

    public async Task<Result<DocumentPreviewResponse>> HandleAsync(
        GetDocumentPreviewQuery query,
        CancellationToken cancellationToken = default)
    {
        Result<DocumentPreviewSource> sourceResult =
            await ResolveSourceAsync(query.DocumentId, cancellationToken);

        if (!sourceResult.IsSuccess)
        {
            return Result<DocumentPreviewResponse>.Failure(sourceResult.Error!);
        }

        DocumentPreviewSource source = sourceResult.Value!;

        if (!source.IsPreviewFileAvailable)
        {
            return Result<DocumentPreviewResponse>.Success(CreateErrorResponse(
                query.DocumentId,
                source.FileName,
                ResolveContentType(source.FileType),
                ErrorCodes.Documents.PreviewFileMissing,
                ErrorMessages.Documents.PreviewFileMissing));
        }

        return source.FileType switch
        {
            FileType.Pdf => Result<DocumentPreviewResponse>.Success(new DocumentPreviewResponse(
                query.DocumentId,
                source.FileName,
                ResolveContentType(source.FileType),
                DocumentPreviewKind.Pdf,
                PreviewUrl: $"/api/v1/documents/{query.DocumentId}/preview/file")),

            FileType.PlainText or FileType.Markdown or FileType.Html =>
                await BuildInlineTextResponseAsync(query.DocumentId, source, cancellationToken),

            FileType.Docx or FileType.Pptx =>
                await BuildConvertedPdfResponseAsync(query.DocumentId, source, cancellationToken),

            FileType.Unknown => Result<DocumentPreviewResponse>.Success(CreateUnsupportedResponse(
                query.DocumentId,
                source.FileName,
                ResolveContentType(source.FileType))),

            _ => Result<DocumentPreviewResponse>.Success(CreateUnsupportedResponse(
                query.DocumentId,
                source.FileName,
                ResolveContentType(source.FileType))),
        };
    }

    public async Task<Result<DocumentPreviewFile>> HandleFileAsync(
        GetDocumentPreviewFileQuery query,
        CancellationToken cancellationToken = default)
    {
        Result<DocumentPreviewSource> sourceResult =
            await ResolveSourceAsync(query.DocumentId, cancellationToken);

        if (!sourceResult.IsSuccess)
        {
            return Result<DocumentPreviewFile>.Failure(sourceResult.Error!);
        }

        DocumentPreviewSource source = sourceResult.Value!;

        if (source.FileType == FileType.Pdf)
        {
            if (!source.IsPreviewFileAvailable)
            {
                return Result<DocumentPreviewFile>.Failure(ApplicationErrors.NotFound(
                    ErrorCodes.Documents.PreviewFileMissing,
                    ErrorMessages.Documents.PreviewFileMissing));
            }

            return Result<DocumentPreviewFile>.Success(new DocumentPreviewFile(
                source.FilePath,
                source.FileName,
                ResolveContentType(source.FileType)));
        }

        if (source.FileType is FileType.Docx or FileType.Pptx)
        {
            if (!source.IsPreviewFileAvailable)
            {
                return Result<DocumentPreviewFile>.Failure(ApplicationErrors.NotFound(
                    ErrorCodes.Documents.PreviewFileMissing,
                    ErrorMessages.Documents.PreviewFileMissing));
            }

            Result<DocumentPreviewConversionResult> conversionResult =
                await ConvertToPdfPreviewAsync(query.DocumentId, source, cancellationToken);

            if (!conversionResult.IsSuccess)
            {
                return Result<DocumentPreviewFile>.Failure(conversionResult.Error!);
            }

            DocumentPreviewConversionResult conversion = conversionResult.Value!;

            return Result<DocumentPreviewFile>.Success(new DocumentPreviewFile(
                conversion.PreviewFilePath,
                conversion.PreviewFileName,
                conversion.ContentType));
        }

        return Result<DocumentPreviewFile>.Failure(ApplicationErrors.UnsupportedMedia(
            ErrorCodes.Documents.PreviewUnsupported,
            ErrorMessages.Documents.PreviewUnsupported));
    }

    private async Task<Result<DocumentPreviewResponse>> BuildConvertedPdfResponseAsync(
        Guid documentId,
        DocumentPreviewSource source,
        CancellationToken cancellationToken)
    {
        Result<DocumentPreviewConversionResult> conversionResult =
            await ConvertToPdfPreviewAsync(documentId, source, cancellationToken);

        if (!conversionResult.IsSuccess)
        {
            // When LibreOffice is not installed or the conversion failed for this specific file,
            // fall back to extracting text directly from the OOXML ZIP archive.
            bool canFallback = conversionResult.Error!.Code is
                ErrorCodes.Documents.PreviewConverterUnavailable or
                ErrorCodes.Documents.PreviewConversionFailed;

            if (canFallback)
            {
                string? html = OfficeDocumentHtmlConverter.TryConvertToHtml(source.FilePath, source.FileType);
                if (html is not null)
                {
                    return Result<DocumentPreviewResponse>.Success(new DocumentPreviewResponse(
                        documentId,
                        source.FileName,
                        "text/html; charset=utf-8",
                        DocumentPreviewKind.Html,
                        TextContent: html));
                }
            }

            return Result<DocumentPreviewResponse>.Success(CreateErrorResponse(
                documentId,
                source.FileName,
                ResolveContentType(source.FileType),
                conversionResult.Error!.Code,
                conversionResult.Error.Message));
        }

        DocumentPreviewConversionResult conversion = conversionResult.Value!;

        return Result<DocumentPreviewResponse>.Success(new DocumentPreviewResponse(
            documentId,
            source.FileName,
            conversion.ContentType,
            DocumentPreviewKind.Pdf,
            PreviewUrl: $"/api/v1/documents/{documentId}/preview/file"));
    }

    private Task<Result<DocumentPreviewConversionResult>> ConvertToPdfPreviewAsync(
        Guid documentId,
        DocumentPreviewSource source,
        CancellationToken cancellationToken)
    {
        return previewConversionService.GetOrCreatePdfPreviewAsync(
            new DocumentPreviewConversionRequest(
                documentId,
                source.FilePath,
                source.FileName,
                source.ContentHash,
                source.FileType),
            cancellationToken);
    }

    private async Task<Result<DocumentPreviewSource>> ResolveSourceAsync(
        Guid documentId,
        CancellationToken cancellationToken)
    {
        Document? document = await documentRepository.GetByIdAsync(documentId, cancellationToken);

        if (document is null)
        {
            return Result<DocumentPreviewSource>.Failure(ApplicationErrors.NotFound(
                ErrorCodes.Documents.NotFound,
                "Document was not found."));
        }

        DocumentFile? file =
            await documentRepository.GetFileByDocumentIdAsync(documentId, cancellationToken);

        if (file is null)
        {
            return Result<DocumentPreviewSource>.Success(new DocumentPreviewSource(
                document.Name,
                string.Empty,
                string.Empty,
                FileType.Unknown,
                IsPreviewFileAvailable: false));
        }

        FileType fileType = file.FileType == FileType.Unknown
            ? DocumentFileTypeResolver.Resolve(file.FileName)
            : file.FileType;

        string? fullPath = ResolveManagedFilePath(file.LocalPath);
        bool isAvailable = fullPath is not null && File.Exists(fullPath);

        return Result<DocumentPreviewSource>.Success(new DocumentPreviewSource(
            file.FileName,
            fullPath ?? string.Empty,
            file.ContentHash,
            fileType,
            isAvailable));
    }

    private async Task<Result<DocumentPreviewResponse>> BuildInlineTextResponseAsync(
        Guid documentId,
        DocumentPreviewSource source,
        CancellationToken cancellationToken)
    {
        FileInfo fileInfo = new(source.FilePath);
        if (fileInfo.Length > MaxInlineTextBytes)
        {
            return Result<DocumentPreviewResponse>.Success(CreateErrorResponse(
                documentId,
                source.FileName,
                ResolveContentType(source.FileType),
                ErrorCodes.Documents.PreviewUnavailable,
                $"Inline preview is limited to {MaxInlineTextBytes / 1024} KB."));
        }

        try
        {
            string textContent = await File.ReadAllTextAsync(
                source.FilePath,
                StrictUtf8,
                cancellationToken);

            return Result<DocumentPreviewResponse>.Success(new DocumentPreviewResponse(
                documentId,
                source.FileName,
                ResolveContentType(source.FileType),
                ResolvePreviewKind(source.FileType),
                TextContent: textContent));
        }
        catch (DecoderFallbackException)
        {
            return Result<DocumentPreviewResponse>.Success(CreateErrorResponse(
                documentId,
                source.FileName,
                ResolveContentType(source.FileType),
                ErrorCodes.Documents.PreviewUnavailable,
                ErrorMessages.Documents.PreviewUnavailable));
        }
        catch (IOException)
        {
            return Result<DocumentPreviewResponse>.Success(CreateErrorResponse(
                documentId,
                source.FileName,
                ResolveContentType(source.FileType),
                ErrorCodes.Documents.PreviewUnavailable,
                ErrorMessages.Documents.PreviewUnavailable));
        }
        catch (UnauthorizedAccessException)
        {
            return Result<DocumentPreviewResponse>.Success(CreateErrorResponse(
                documentId,
                source.FileName,
                ResolveContentType(source.FileType),
                ErrorCodes.Documents.PreviewUnavailable,
                ErrorMessages.Documents.PreviewUnavailable));
        }
    }

    private string? ResolveManagedFilePath(string localPath)
    {
        if (string.IsNullOrWhiteSpace(localPath))
        {
            return null;
        }

        string fullLocalPath;
        string fullFilesPath;

        try
        {
            fullLocalPath = Path.GetFullPath(localPath);
            fullFilesPath = Path.GetFullPath(paths.FilesDirectory);
        }
        catch (Exception exception) when (exception is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return null;
        }

        return fullLocalPath.StartsWith(fullFilesPath + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
            ? fullLocalPath
            : null;
    }

    private static DocumentPreviewResponse CreateErrorResponse(
        Guid documentId,
        string fileName,
        string contentType,
        string errorCode,
        string message)
    {
        return new DocumentPreviewResponse(
            documentId,
            fileName,
            contentType,
            DocumentPreviewKind.Error,
            ErrorCode: errorCode,
            Message: message);
    }

    private static DocumentPreviewResponse CreateUnsupportedResponse(
        Guid documentId,
        string fileName,
        string contentType)
    {
        return new DocumentPreviewResponse(
            documentId,
            fileName,
            contentType,
            DocumentPreviewKind.Unsupported,
            ErrorCode: ErrorCodes.Documents.PreviewUnsupported,
            Message: ErrorMessages.Documents.PreviewUnsupported);
    }

    private static DocumentPreviewKind ResolvePreviewKind(FileType fileType) => fileType switch
    {
        FileType.PlainText => DocumentPreviewKind.Text,
        FileType.Markdown => DocumentPreviewKind.Markdown,
        FileType.Html => DocumentPreviewKind.Html,
        _ => DocumentPreviewKind.Unsupported,
    };

    private static string ResolveContentType(FileType fileType) => fileType switch
    {
        FileType.Pdf => "application/pdf",
        FileType.PlainText => "text/plain; charset=utf-8",
        FileType.Markdown => "text/markdown; charset=utf-8",
        FileType.Html => "text/html; charset=utf-8",
        FileType.Docx => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        FileType.Pptx => "application/vnd.openxmlformats-officedocument.presentationml.presentation",
        _ => "application/octet-stream",
    };

    private sealed record DocumentPreviewSource(
        string FileName,
        string FilePath,
        string ContentHash,
        FileType FileType,
        bool IsPreviewFileAvailable);
}
