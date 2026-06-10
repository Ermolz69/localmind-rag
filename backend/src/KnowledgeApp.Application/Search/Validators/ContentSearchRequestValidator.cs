using KnowledgeApp.Application.Common.Errors;
using KnowledgeApp.Application.Common.Results;
using KnowledgeApp.Contracts.Search;

namespace KnowledgeApp.Application.Search;

public sealed class ContentSearchRequestValidator
{
    public Result Validate(ContentSearchRequest request)
    {
        Dictionary<string, string[]> errors = [];

        if (string.IsNullOrWhiteSpace(request.Query))
        {
            errors["query"] = ["Search query is required."];
        }
        else if (request.Query.Length > 4_000)
        {
            errors["query"] = ["Search query must be 4000 characters or less."];
        }

        if (request.Limit < 1 || request.Limit > 50)
        {
            errors["limit"] = ["Search limit must be between 1 and 50."];
        }

        if (!request.IncludeDocuments && !request.IncludeNotes)
        {
            errors["sources"] = ["At least one search source must be enabled."];
        }

        if (request.DocumentId.HasValue && !request.IncludeDocuments)
        {
            errors["documentId"] = ["DocumentId cannot be used when document search is disabled."];
        }

        if (request.NoteId.HasValue && !request.IncludeNotes)
        {
            errors["noteId"] = ["NoteId cannot be used when note search is disabled."];
        }

        if (request.FileType is { } fileType && !FileTypeParser.IsValid(fileType))
        {
            errors["fileType"] = ["File type filter must be one of: pdf, docx, pptx, markdown, txt, html."];
        }

        if (request.DateFrom.HasValue && request.DateTo.HasValue && request.DateFrom.Value > request.DateTo.Value)
        {
            errors["dateFrom"] = ["DateFrom must be less than or equal to DateTo."];
        }

        if (errors.Count > 0)
        {
            return Result.Failure(ApplicationErrors.Validation(
                ErrorCodes.Search.ValidationFailed,
                "Content search request is invalid.",
                errors));
        }

        return Result.Success();
    }
}
