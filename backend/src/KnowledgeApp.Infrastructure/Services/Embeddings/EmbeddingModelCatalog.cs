using System.Reflection;
using System.Text.Json;
using KnowledgeApp.Infrastructure.Options;
using Microsoft.Extensions.Options;

namespace KnowledgeApp.Infrastructure.Services;

public sealed class EmbeddingModelCatalog
{
    private const string ResourcePrefix = "KnowledgeApp.Infrastructure.Resources.AiModels.";
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
    private readonly Lazy<IReadOnlyDictionary<string, EmbeddingModelManifest>> manifests;
    private readonly AiOptions options;

    public EmbeddingModelCatalog(IOptions<AiOptions> options)
    {
        this.options = options.Value;
        manifests = new Lazy<IReadOnlyDictionary<string, EmbeddingModelManifest>>(LoadManifests);
    }

    public IReadOnlyCollection<EmbeddingModelManifest> List() => manifests.Value.Values.ToArray();

    public EmbeddingModelManifest GetDefault()
    {
        string manifestId = string.IsNullOrWhiteSpace(options.EmbeddingModelManifest)
            ? "bge-m3-q4-k-m"
            : options.EmbeddingModelManifest;

        return GetById(manifestId);
    }

    public EmbeddingModelManifest GetById(string id)
    {
        if (manifests.Value.TryGetValue(id, out EmbeddingModelManifest? manifest))
        {
            return manifest;
        }

        throw new InvalidOperationException($"Embedding model manifest '{id}' was not found.");
    }

    private static IReadOnlyDictionary<string, EmbeddingModelManifest> LoadManifests()
    {
        Assembly assembly = typeof(EmbeddingModelCatalog).Assembly;
        Dictionary<string, EmbeddingModelManifest> loaded = new(StringComparer.OrdinalIgnoreCase);

        foreach (string resourceName in assembly.GetManifestResourceNames().Where(x => x.StartsWith(ResourcePrefix, StringComparison.Ordinal)))
        {
            using Stream? stream = assembly.GetManifestResourceStream(resourceName);
            if (stream is null)
            {
                continue;
            }

            EmbeddingModelManifest? manifest = JsonSerializer.Deserialize<EmbeddingModelManifest>(stream, SerializerOptions);
            if (manifest is null || string.IsNullOrWhiteSpace(manifest.Id))
            {
                continue;
            }

            loaded[manifest.Id] = manifest;
        }

        if (loaded.Count == 0)
        {
            throw new InvalidOperationException("No embedding model manifests were found.");
        }

        return loaded;
    }
}
