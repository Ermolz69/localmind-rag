using KnowledgeApp.Infrastructure.Options;
using Microsoft.Extensions.Options;

namespace KnowledgeApp.Infrastructure.Options.Validation;

public sealed class StorageOptionsValidator : IValidateOptions<StorageOptions>
{
    public ValidateOptionsResult Validate(string? name, StorageOptions options)
    {
        List<string> failures = [];

        if (!OptionsValidationRules.IsValidPath(options.DataPath))
        {
            failures.Add("LocalRuntime:DataPath must be a valid non-empty directory path.");
        }

        if (!OptionsValidationRules.IsValidPath(options.FilesPath))
        {
            failures.Add("LocalRuntime:FilesPath must be a valid non-empty directory path.");
        }

        if (!OptionsValidationRules.IsValidPath(options.PreviewsPath))
        {
            failures.Add("LocalRuntime:PreviewsPath must be a valid non-empty directory path.");
        }

        if (!OptionsValidationRules.IsValidPath(options.LogsPath))
        {
            failures.Add("LocalRuntime:LogsPath must be a valid non-empty directory path.");
        }

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }
}
