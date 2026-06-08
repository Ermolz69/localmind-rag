using KnowledgeApp.Infrastructure.Options;
using Microsoft.Extensions.Options;

namespace KnowledgeApp.Infrastructure.Options.Validation;

public sealed class ChunkingOptionsValidator : IValidateOptions<ChunkingOptions>
{
    public ValidateOptionsResult Validate(string? name, ChunkingOptions options)
    {
        List<string> failures = [];

        if (!string.Equals(options.Strategy, "StructureAware", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(options.Strategy, "Simple", StringComparison.OrdinalIgnoreCase))
        {
            failures.Add("Chunking:Strategy must be StructureAware or Simple.");
        }

        if (options.TargetChunkCharacters <= 0)
        {
            failures.Add("Chunking:TargetChunkCharacters must be greater than zero.");
        }

        if (options.MaxChunkCharacters < options.TargetChunkCharacters)
        {
            failures.Add("Chunking:MaxChunkCharacters must be greater than or equal to TargetChunkCharacters.");
        }

        if (options.MinChunkCharacters < 0)
        {
            failures.Add("Chunking:MinChunkCharacters must be greater than or equal to zero.");
        }

        if (options.OverlapCharacters < 0 || options.OverlapCharacters >= options.TargetChunkCharacters)
        {
            failures.Add("Chunking:OverlapCharacters must be greater than or equal to zero and less than TargetChunkCharacters.");
        }

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }
}
