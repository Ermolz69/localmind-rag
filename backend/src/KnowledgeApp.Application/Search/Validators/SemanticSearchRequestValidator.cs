using KnowledgeApp.Application.Common.Errors;
using KnowledgeApp.Application.Common.Results;
using KnowledgeApp.Contracts.Rag;

namespace KnowledgeApp.Application.Search;

public sealed class SemanticSearchRequestValidator
{
    public const int DefaultLimit = 8;
    public const int MaxLimit = 50;
    public const int MaxQueryLength = 4_000;

    public const string QueryField = "query";
    public const string LimitField = "limit";

    public Result Validate(SemanticSearchRequest request)
    {
        Dictionary<string, string[]> errors = [];

        if (string.IsNullOrWhiteSpace(request.Query))
        {
            errors[QueryField] = [ErrorMessages.Search.QueryRequired];
        }
        else if (request.Query.Length > MaxQueryLength)
        {
            errors[QueryField] = [ErrorMessages.Search.QueryTooLong];
        }

        if (request.Limit < 1 || request.Limit > MaxLimit)
        {
            errors[LimitField] = [ErrorMessages.Search.LimitOutOfRange];
        }

        if (errors.Count > 0)
        {
            return Result.Failure(ApplicationErrors.Validation(
                ErrorCodes.Search.ValidationFailed,
                ErrorMessages.Search.RequestInvalid,
                errors));
        }

        return Result.Success();
    }
}
