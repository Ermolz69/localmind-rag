using KnowledgeApp.Application.Common.Errors;
using KnowledgeApp.Application.Common.Results;

namespace KnowledgeApp.Application.Documents;

public sealed class UploadDocumentCommandValidator
{
    public const long MaxFileSizeBytes = 100L * 1024 * 1024;

    public Result Validate(UploadDocumentCommand command)
    {
        ArgumentNullException.ThrowIfNull(command.Content);

        if (string.IsNullOrWhiteSpace(command.FileName))
        {
            return Result.Failure(ApplicationErrors.Validation(
                ErrorCodes.Documents.FileNameRequired,
                ErrorMessages.Documents.FileNameRequired,
                new Dictionary<string, string[]> { ["fileName"] = [ErrorMessages.Documents.FileNameRequired] }));
        }

        if (command.Length <= 0)
        {
            return Result.Failure(ApplicationErrors.Validation(
                ErrorCodes.Documents.FileEmpty,
                ErrorMessages.Documents.FileEmpty,
                new Dictionary<string, string[]> { ["file"] = [ErrorMessages.Documents.FileEmpty] }));
        }

        if (command.Length > MaxFileSizeBytes)
        {
            return Result.Failure(ApplicationErrors.Validation(
                ErrorCodes.Documents.FileTooLarge,
                ErrorMessages.Documents.FileTooLarge,
                new Dictionary<string, string[]> { ["file"] = [ErrorMessages.Documents.FileTooLarge] }));
        }

        if (!DocumentFileTypeResolver.IsSupported(command.FileName))
        {
            return Result.Failure(ApplicationErrors.Validation(
                ErrorCodes.Documents.UnsupportedFileType,
                ErrorMessages.Documents.UnsupportedFileType,
                new Dictionary<string, string[]> { ["fileName"] = [ErrorMessages.Documents.UnsupportedFileType] }));
        }

        return Result.Success();
    }
}
