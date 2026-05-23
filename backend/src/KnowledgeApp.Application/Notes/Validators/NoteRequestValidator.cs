using KnowledgeApp.Application.Common.Errors;
using KnowledgeApp.Application.Exceptions;
using KnowledgeApp.Contracts.Notes;

namespace KnowledgeApp.Application.Notes;

public sealed class NoteRequestValidator
{
    public void Validate(CreateNoteRequest request)
    {
        ValidateTitleAndMarkdown(request.Title, request.Markdown);
    }

    public void Validate(UpdateNoteRequest request)
    {
        ValidateTitleAndMarkdown(request.Title, request.Markdown);
    }

    private static void ValidateTitleAndMarkdown(string title, string markdown)
    {
        Dictionary<string, string[]> errors = [];

        if (string.IsNullOrWhiteSpace(title))
        {
            errors["title"] = [ErrorMessages.Notes.TitleRequired];
        }
        else if (title.Length > 200)
        {
            errors["title"] = [ErrorMessages.Notes.TitleTooLong];
        }

        if (markdown.Length > 1_000_000)
        {
            errors["markdown"] = [ErrorMessages.Notes.MarkdownTooLong];
        }

        if (errors.Count > 0)
        {
            throw new ValidationAppException(ErrorCodes.Notes.ValidationFailed, ErrorMessages.Notes.RequestInvalid, errors);
        }
    }
}
