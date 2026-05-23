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
public sealed record RuntimeStatusDto(
    bool LocalApiReady,
    string AiRuntimeStatus,
    bool ModelsAvailable,
    bool OfflineMode,
    string? RuntimePath = null,
    string? ModelPath = null,
    bool SetupRequired = false,
    string? SetupReason = null);
