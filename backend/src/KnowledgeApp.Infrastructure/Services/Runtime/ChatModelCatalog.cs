using System.Reflection;
using System.Text.Json;

using KnowledgeApp.Infrastructure.Options;

using Microsoft.Extensions.Options;

namespace KnowledgeApp.Infrastructure.Services;

public sealed class ChatModelCatalog
{
    private const string ResourcePrefix =
        "KnowledgeApp.Infrastructure.Resources.AiModels.";

    private static readonly JsonSerializerOptions SerializerOptions =
        new(JsonSerializerDefaults.Web);

    private readonly Lazy<IReadOnlyDictionary<string, ChatModelManifest>> manifests;
    private readonly RuntimeOptions options;

    public ChatModelCatalog(IOptions<RuntimeOptions> options)
    {
        this.options = options.Value;
        manifests = new Lazy<IReadOnlyDictionary<string, ChatModelManifest>>(
            LoadManifests);
    }

    public IReadOnlyCollection<ChatModelManifest> List()
    {
        return manifests.Value.Values.ToArray();
    }

    public ChatModelManifest GetDefault()
    {
        string manifestId = string.IsNullOrWhiteSpace(options.ChatModelManifest)
            ? "llama-3.2-3b-instruct-q4-k-m"
            : options.ChatModelManifest;

        return GetById(manifestId);
    }

    public ChatModelManifest GetById(string id)
    {
        if (manifests.Value.TryGetValue(id, out ChatModelManifest? manifest))
        {
            return manifest;
        }

        throw new InvalidOperationException(
            $"Chat model manifest '{id}' was not found.");
    }

    private static IReadOnlyDictionary<string, ChatModelManifest> LoadManifests()
    {
        Assembly assembly = typeof(ChatModelCatalog).Assembly;

        Dictionary<string, ChatModelManifest> loaded =
            new(StringComparer.OrdinalIgnoreCase);

        foreach (string resourceName in assembly.GetManifestResourceNames()
            .Where(resourceName =>
                resourceName.StartsWith(ResourcePrefix, StringComparison.Ordinal)))
        {
            using Stream? stream = assembly.GetManifestResourceStream(resourceName);

            if (stream is null)
            {
                continue;
            }

            ChatModelManifest? manifest =
                JsonSerializer.Deserialize<ChatModelManifest>(
                    stream,
                    SerializerOptions);

            if (manifest is null
                || string.IsNullOrWhiteSpace(manifest.Id)
                || string.IsNullOrWhiteSpace(manifest.BaseModel))
            {
                continue;
            }

            loaded[manifest.Id] = manifest;
        }

        if (loaded.Count == 0)
        {
            throw new InvalidOperationException(
                "No chat model manifests were found.");
        }

        return loaded;
    }
}
