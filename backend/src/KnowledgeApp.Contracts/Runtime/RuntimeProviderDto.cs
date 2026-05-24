namespace KnowledgeApp.Contracts.Runtime;

/// <summary>AI runtime provider visible to LocalApi clients.</summary>
/// <param name="Id">Stable provider identifier.</param>
/// <param name="Name">Human-readable provider name.</param>
/// <param name="Selected">True when this provider is currently selected.</param>
/// <param name="Status">Current provider status.</param>
/// <param name="Capabilities">Capabilities advertised by this provider.</param>
/// <param name="BaseUrl">Provider base URL when the provider exposes an HTTP API.</param>
/// <param name="FailureReason">Sanitized setup or availability reason when not healthy.</param>
public sealed record RuntimeProviderDto(
    string Id,
    string Name,
    bool Selected,
    string Status,
    AiRuntimeProviderCapabilities Capabilities,
    string? BaseUrl = null,
    string? FailureReason = null);
