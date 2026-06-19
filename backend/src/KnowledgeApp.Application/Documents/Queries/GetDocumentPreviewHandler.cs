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
    IAppPathProvider paths)
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

            FileType.Docx or FileType.Pptx or FileType.Unknown =>
                Result<DocumentPreviewResponse>.Success(new DocumentPreviewResponse(
                    query.DocumentId,
                    source.FileName,
                    ResolveContentType(source.FileType),
                    DocumentPreviewKind.Unsupported,
                    ErrorCode: ErrorCodes.Documents.PreviewUnsupported,
                    Message: ErrorMessages.Documents.PreviewUnsupported)),

            _ => Result<DocumentPreviewResponse>.Success(new DocumentPreviewResponse(
                query.DocumentId,
                source.FileName,
                ResolveContentType(source.FileType),
                DocumentPreviewKind.Unsupported,
                ErrorCode: ErrorCodes.Documents.PreviewUnsupported,
                Message: ErrorMessages.Documents.PreviewUnsupported)),
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

        if (source.FileType != FileType.Pdf)
        {
            return Result<DocumentPreviewFile>.Failure(ApplicationErrors.UnsupportedMedia(
                ErrorCodes.Documents.PreviewUnsupported,
                ErrorMessages.Documents.PreviewUnsupported));
        }

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
        _ => "application/octet-stream",
    };

    private sealed record DocumentPreviewSource(
        string FileName,
        string FilePath,
        FileType FileType,
        bool IsPreviewFileAvailable);
}
