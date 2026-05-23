using KnowledgeApp.Application.Common.Errors;
using KnowledgeApp.Application.Exceptions;
using KnowledgeApp.Contracts.Buckets;

namespace KnowledgeApp.Application.Buckets;

public sealed class BucketRequestValidator
{
    public void Validate(CreateBucketRequest request)
    {
        ValidateNameAndDescription(request.Name, request.Description);
    }

    public void Validate(UpdateBucketRequest request)
    {
        ValidateNameAndDescription(request.Name, request.Description);
    }

    private static void ValidateNameAndDescription(string name, string? description)
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
            throw new ValidationAppException(ErrorCodes.Buckets.ValidationFailed, ErrorMessages.Buckets.RequestInvalid, errors);
        }
    }
}
