namespace KnowledgeApp.Contracts.Runtime;

/// <summary>Current status of LocalApi and the local AI runtime.</summary>
/// <param name="LocalApiReady">True when LocalApi is ready to serve desktop requests.</param>
/// <param name="AiRuntimeStatus">Current AI runtime status value.</param>
/// <param name="ModelsAvailable">True when required local models are available.</param>
/// <param name="OfflineMode">True when LocalMind is operating without remote connectivity.</param>
/// <param name="RuntimePath">Configured local AI runtime executable path.</param>
/// <param name="ModelPath">Configured local model path.</param>
/// <param name="SetupRequired">True when runtime or model setup is incomplete.</param>
/// <param name="SetupReason">Human-readable setup guidance when setup is required.</param>
/// <param name="ProviderId">Stable AI runtime provider identifier.</param>
/// <param name="ProviderName">Display name for the configured provider.</param>
/// <param name="ProviderStatus">Stable provider status value.</param>
/// <param name="Capabilities">Capabilities advertised by the provider.</param>
/// <param name="BaseUrl">Provider API base URL when applicable.</param>
/// <param name="FailureReason">Sanitized provider failure or setup reason.</param>
/// <param name="ChatModelName">Configured chat model name.</param>
/// <param name="EmbeddingModelName">Configured embedding model name.</param>
/// <param name="ChatModelPath">Resolved local chat model path.</param>
/// <param name="EmbeddingModelPath">Resolved local embedding model path.</param>
public sealed record RuntimeStatusDto(
    bool LocalApiReady,
    string AiRuntimeStatus,
    bool ModelsAvailable,
    bool OfflineMode,
    string? RuntimePath = null,
    string? ModelPath = null,
    bool SetupRequired = false,
    string? SetupReason = null,
    string ProviderId = "llama-cpp",
    string ProviderName = "llama.cpp",
    string ProviderStatus = AiRuntimeProviderStatus.Missing,
    AiRuntimeProviderCapabilities? Capabilities = null,
    string? BaseUrl = null,
    string? FailureReason = null,
    string? ChatModelName = null,
    string? EmbeddingModelName = null,
    string? ChatModelPath = null,
    string? EmbeddingModelPath = null);
