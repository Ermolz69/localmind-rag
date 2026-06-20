using KnowledgeApp.Infrastructure.Options;
using Microsoft.Extensions.Options;

namespace KnowledgeApp.Infrastructure.Options.Validation;

public sealed class DocumentPreviewOptionsValidator : IValidateOptions<DocumentPreviewOptions>
{
    public ValidateOptionsResult Validate(string? name, DocumentPreviewOptions options)
    {
        if (options.ConversionTimeoutSeconds <= 0)
        {
            return ValidateOptionsResult.Fail(
                "DocumentPreview:ConversionTimeoutSeconds must be greater than zero.");
        }

        return ValidateOptionsResult.Success;
    }
}
