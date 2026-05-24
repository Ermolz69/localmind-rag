using KnowledgeApp.Application.Common.Errors;
using KnowledgeApp.Application.Common.Results;
using KnowledgeApp.Contracts.Buckets;

namespace KnowledgeApp.Application.Buckets;

public sealed class BucketRequestValidator
{
    public Result Validate(CreateBucketRequest request)
    {
        return ValidateNameAndDescription(request.Name, request.Description);
    }

    public Result Validate(UpdateBucketRequest request)
    {
        return ValidateNameAndDescription(request.Name, request.Description);
    }

    private static Result ValidateNameAndDescription(string name, string? description)
    {
        Dictionary<string, string[]> errors = [];

        if (string.IsNullOrWhiteSpace(name))
        {
            errors["name"] = [ErrorMessages.Buckets.NameRequired];
        }
        else if (name.Length > 120)
        {
            errors["name"] = [ErrorMessages.Buckets.NameTooLong];
        }

        if (description is { Length: > 500 })
        {
            errors["description"] = [ErrorMessages.Buckets.DescriptionTooLong];
        }

        if (errors.Count > 0)
        {
            return Result.Failure(ApplicationErrors.Validation(
                ErrorCodes.Buckets.ValidationFailed,
                ErrorMessages.Buckets.RequestInvalid,
                errors));
        }

        return Result.Success();
    }
}
