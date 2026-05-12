namespace KnowledgeApp.Application.Documents;

public sealed class UploadDocumentCommandValidator
{
    public const long MaxFileSizeBytes = 100L * 1024 * 1024;

    public void Validate(UploadDocumentCommand command)
    {
        ArgumentNullException.ThrowIfNull(command.Content);

        if (string.IsNullOrWhiteSpace(command.FileName))
        {
            throw new ArgumentException("Document file name is required.", nameof(command));
        }

        if (command.Length <= 0)
        {
            throw new ArgumentException("Document file must not be empty.", nameof(command));
        }

        if (command.Length > MaxFileSizeBytes)
        {
            throw new ArgumentException("Document file size must be less than or equal to 100 MB.", nameof(command));
        }

        if (!DocumentFileTypeResolver.IsSupported(command.FileName))
        {
            throw new ArgumentException("Document file extension is not supported.", nameof(command));
        }
    }
}
