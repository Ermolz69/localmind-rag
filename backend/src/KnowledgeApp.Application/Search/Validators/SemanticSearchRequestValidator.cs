using KnowledgeApp.Application.Exceptions;
using KnowledgeApp.Contracts.Rag;

namespace KnowledgeApp.Application.Search;

public sealed class SemanticSearchRequestValidator
{
    public void Validate(SemanticSearchRequest request)
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

        if (errors.Count > 0)
        {
            throw new ValidationAppException("search.validationFailed", "Semantic search request is invalid.", errors);
        }
    }
}
