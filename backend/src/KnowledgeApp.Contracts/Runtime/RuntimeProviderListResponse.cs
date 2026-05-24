namespace KnowledgeApp.Contracts.Runtime;

/// <summary>List of AI runtime providers known to LocalApi.</summary>
/// <param name="SelectedProviderId">Identifier of the configured provider.</param>
/// <param name="Providers">Known providers with capabilities and status.</param>
public sealed record RuntimeProviderListResponse(
    string SelectedProviderId,
    IReadOnlyList<RuntimeProviderDto> Providers);
