using KnowledgeApp.Infrastructure.Options;
using Microsoft.Extensions.Options;

namespace KnowledgeApp.Infrastructure.Options.Validation;

public sealed class VectorIndexOptionsValidator : IValidateOptions<VectorIndexOptions>
{
    public ValidateOptionsResult Validate(string? name, VectorIndexOptions options)
    {
        List<string> failures = [];

        if (!OptionsValidationRules.IsValidPath(options.IndexPath))
        {
            failures.Add("LocalRuntime:IndexPath must be a valid non-empty directory path.");
        }

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }
}
