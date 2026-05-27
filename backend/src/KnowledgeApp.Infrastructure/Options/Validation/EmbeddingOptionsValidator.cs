using KnowledgeApp.Infrastructure.Options;
using Microsoft.Extensions.Options;

namespace KnowledgeApp.Infrastructure.Options.Validation;

public sealed class EmbeddingOptionsValidator : IValidateOptions<EmbeddingOptions>
{
    private static readonly HashSet<string> SupportedProviders =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "LlamaCpp",
            "llama-cpp",
            "Stub",
        };

    public ValidateOptionsResult Validate(string? name, EmbeddingOptions options)
    {
        List<string> failures = [];

        if (OptionsValidationRules.IsMissing(options.EmbeddingProvider))
        {
            failures.Add("Ai:EmbeddingProvider is required.");
        }
        else if (!SupportedProviders.Contains(options.EmbeddingProvider))
        {
            failures.Add("Ai:EmbeddingProvider must be one of: LlamaCpp or Stub.");
        }

        if (OptionsValidationRules.IsMissing(options.EmbeddingModel))
        {
            failures.Add("Ai:EmbeddingModel is required.");
        }

        if (OptionsValidationRules.IsMissing(options.EmbeddingModelManifest))
        {
            failures.Add("Ai:EmbeddingModelManifest is required.");
        }

        if (!OptionsValidationRules.IsValidPath(options.ModelsPath))
        {
            failures.Add("Ai:ModelsPath must be a valid non-empty directory path.");
        }

        if (options.TopK <= 0)
        {
            failures.Add("Ai:TopK must be greater than zero.");
        }

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }
}
