using KnowledgeApp.Infrastructure.Options;
using Microsoft.Extensions.Options;

namespace KnowledgeApp.Infrastructure.Options.Validation;

public sealed class ChunkingOptionsValidator : IValidateOptions<ChunkingOptions>
{
    public ValidateOptionsResult Validate(string? name, ChunkingOptions options)
    {
        List<string> failures = [];

        if (string.IsNullOrWhiteSpace(options.Tokenizer.TokenizerId))
        {
            failures.Add("Chunking:Tokenizer:TokenizerId is required.");
        }

        if (string.IsNullOrWhiteSpace(options.ChunkingAlgorithmId))
        {
            failures.Add("Chunking:ChunkingAlgorithmId is required.");
        }

        ValidateProfile(options.Default, "Default", failures);
        ValidateProfile(options.Code, "Code", failures);
        ValidateProfile(options.Table, "Table", failures);
        ValidateProfile(options.Slide, "Slide", failures);

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }

    private static void ValidateProfile(ChunkingProfile profile, string profileName, List<string> failures)
    {
        if (profile.TargetTokens <= 0)
        {
            failures.Add($"Chunking:{profileName}:TargetTokens must be greater than zero.");
        }

        if (profile.MaxTokens < profile.TargetTokens)
        {
            failures.Add($"Chunking:{profileName}:MaxTokens must be greater than or equal to TargetTokens.");
        }

        if (profile.MinTokens < 0)
        {
            failures.Add($"Chunking:{profileName}:MinTokens must be greater than or equal to zero.");
        }

        if (profile.OverlapTokens < 0 || profile.OverlapTokens >= profile.TargetTokens)
        {
            failures.Add($"Chunking:{profileName}:OverlapTokens must be greater than or equal to zero and less than TargetTokens.");
        }
    }
}
