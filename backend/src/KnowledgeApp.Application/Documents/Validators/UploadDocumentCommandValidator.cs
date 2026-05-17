using KnowledgeApp.Application.Exceptions;

namespace KnowledgeApp.Application.Documents;

public sealed class UploadDocumentCommandValidator
{
    public const long MaxFileSizeBytes = 100L * 1024 * 1024;

    public void Validate(UploadDocumentCommand command)
    {
        ArgumentNullException.ThrowIfNull(command.Content);

        if (string.IsNullOrWhiteSpace(command.FileName))
        {
            throw new ValidationAppException(
                "documents.fileNameRequired",
                "Document file name is required.",
                new Dictionary<string, string[]> { ["fileName"] = ["Document file name is required."] });
        }

        if (command.Length <= 0)
        {
            throw new ValidationAppException(
                "documents.fileEmpty",
                "Document file must not be empty.",
                new Dictionary<string, string[]> { ["file"] = ["Document file must not be empty."] });
        }

        if (command.Length > MaxFileSizeBytes)
        {
            throw new ValidationAppException(
                "documents.fileTooLarge",
                "Document file size must be less than or equal to 100 MB.",
                new Dictionary<string, string[]> { ["file"] = ["Document file size must be less than or equal to 100 MB."] });
        }

        if (!DocumentFileTypeResolver.IsSupported(command.FileName))
        {
            throw new ValidationAppException(
                "documents.unsupportedFileType",
                "Document file extension is not supported.",
                new Dictionary<string, string[]> { ["fileName"] = ["Document file extension is not supported."] });
        }
    }
}
