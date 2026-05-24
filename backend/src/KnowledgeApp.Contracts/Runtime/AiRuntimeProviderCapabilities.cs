namespace KnowledgeApp.Contracts.Runtime;

/// <summary>Capabilities advertised by the configured AI runtime provider.</summary>
public sealed record AiRuntimeProviderCapabilities(
    bool SupportsEmbeddings,
    bool SupportsChat,
    bool SupportsModelListing,
    bool SupportsSetup,
    bool SupportsStart,
    bool SupportsStop);
