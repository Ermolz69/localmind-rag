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
            errors["title"] = ["Note title is required."];
        }
        else if (title.Length > 200)
        {
            errors["title"] = ["Note title must be 200 characters or less."];
        }

        if (markdown.Length > 1_000_000)
        {
            errors["markdown"] = ["Note markdown must be 1000000 characters or less."];
        }

        if (errors.Count > 0)
        {
            throw new ValidationAppException("notes.validationFailed", "Note request is invalid.", errors);
        }
    }
}
