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
            errors["name"] = ["Bucket name is required."];
        }
        else if (name.Length > 120)
        {
            errors["name"] = ["Bucket name must be 120 characters or less."];
        }

        if (description is { Length: > 500 })
        {
            errors["description"] = ["Bucket description must be 500 characters or less."];
        }

        if (errors.Count > 0)
        {
            throw new ValidationAppException("buckets.validationFailed", "Bucket request is invalid.", errors);
        }
    }
}
