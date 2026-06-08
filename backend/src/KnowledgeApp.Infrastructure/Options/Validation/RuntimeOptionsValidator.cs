using KnowledgeApp.Infrastructure.Options;
using Microsoft.Extensions.Options;

namespace KnowledgeApp.Infrastructure.Options.Validation;

public sealed class RuntimeOptionsValidator : IValidateOptions<RuntimeOptions>
{
    private static readonly HashSet<string> SupportedProviders =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "LlamaCpp",
            "llama-cpp",
            "Stub",
        };

    public ValidateOptionsResult Validate(string? name, RuntimeOptions options)
    {
        List<string> failures = [];

        if (OptionsValidationRules.IsMissing(options.Provider))
        {
            failures.Add("Ai:Provider is required.");
        }
        else if (!SupportedProviders.Contains(options.Provider))
        {
            failures.Add("Ai:Provider must be one of: LlamaCpp or Stub.");
        }

        if (!OptionsValidationRules.IsAbsoluteHttpUrl(options.BaseUrl))
        {
            failures.Add("Ai:BaseUrl must be an absolute HTTP or HTTPS URL.");
        }

        if (!OptionsValidationRules.IsAbsoluteHttpUrl(options.ChatBaseUrl))
        {
            failures.Add("Ai:ChatBaseUrl must be an absolute HTTP or HTTPS URL.");
        }

        if (!OptionsValidationRules.IsAbsoluteHttpUrl(options.EmbeddingBaseUrl))
        {
            failures.Add("Ai:EmbeddingBaseUrl must be an absolute HTTP or HTTPS URL.");
        }

        if (OptionsValidationRules.IsMissing(options.ChatModel))
        {
            failures.Add("Ai:ChatModel is required.");
        }

        if (OptionsValidationRules.IsMissing(options.ChatModelManifest))
        {
            failures.Add("Ai:ChatModelManifest is required.");
        }

        if (options.Temperature < 0 || options.Temperature > 2)
        {
            failures.Add("Ai:Temperature must be between 0 and 2.");
        }

        if (options.ContextSize <= 0)
        {
            failures.Add("Ai:ContextSize must be greater than zero.");
        }

        if (!OptionsValidationRules.IsValidPath(options.RuntimePath))
        {
            failures.Add("Ai:RuntimePath must be a valid non-empty file path.");
        }

        if (!OptionsValidationRules.IsAbsoluteHttpUrl(options.RuntimeDownloadUrl))
        {
            failures.Add("Ai:RuntimeDownloadUrl must be an absolute HTTP or HTTPS URL.");
        }

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }
}
