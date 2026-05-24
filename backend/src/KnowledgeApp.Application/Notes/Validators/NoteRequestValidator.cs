using KnowledgeApp.Application.Common.Errors;
using KnowledgeApp.Application.Common.Results;
using KnowledgeApp.Contracts.Notes;

namespace KnowledgeApp.Application.Notes;

public sealed class NoteRequestValidator
{
    public Result Validate(CreateNoteRequest request)
    {
        return ValidateTitleAndMarkdown(request.Title, request.Markdown);
    }

    public Result Validate(UpdateNoteRequest request)
    {
        return ValidateTitleAndMarkdown(request.Title, request.Markdown);
    }

    private static Result ValidateTitleAndMarkdown(string title, string markdown)
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
            return Result.Failure(ApplicationErrors.Validation(
                ErrorCodes.Notes.ValidationFailed,
                ErrorMessages.Notes.RequestInvalid,
                errors));
        }

        return Result.Success();
    }
}
