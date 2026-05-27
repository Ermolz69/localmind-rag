using KnowledgeApp.Infrastructure.Options;

using Microsoft.Extensions.Options;

namespace KnowledgeApp.Infrastructure.Options.Validation;

public sealed class RagOptionsValidator : IValidateOptions<RagOptions>
{
    public ValidateOptionsResult Validate(string? name, RagOptions options)
    {
        if (options.MinimumSourceScore < 0 || options.MinimumSourceScore > 1)
        {
            return ValidateOptionsResult.Fail(
                "Rag:MinimumSourceScore must be between 0 and 1.");
        }

        return ValidateOptionsResult.Success;
    }
}
