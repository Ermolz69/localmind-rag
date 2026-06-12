namespace KnowledgeApp.Contracts.Runtime;

/// <summary>Represents the response from starting an AI runtime setup.</summary>
/// <param name="SetupId">The unique identifier of the started setup session.</param>
/// <param name="AlreadyRunning">True if the setup was already running, false if a new session was started.</param>
/// <param name="RuntimeInstalled">True when the runtime executable is installed.</param>
/// <param name="ModelInstalled">True when the configured model is installed.</param>
/// <param name="Message">A user-safe setup status message.</param>
/// <param name="Status">The current runtime status, when available.</param>
public sealed record RuntimeSetupStartedResponse(
    Guid SetupId,
    bool AlreadyRunning,
    bool RuntimeInstalled = true,
    bool ModelInstalled = true,
    string Message = "Background setup started",
    RuntimeStatusDto? Status = null);
