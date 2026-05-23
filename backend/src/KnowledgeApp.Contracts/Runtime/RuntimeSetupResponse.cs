namespace KnowledgeApp.Contracts.Runtime;

/// <summary>Result returned after attempting local AI runtime setup.</summary>
/// <param name="RuntimeInstalled">True when the runtime executable is installed.</param>
/// <param name="ModelInstalled">True when the required model files are installed.</param>
/// <param name="Message">Human-readable setup result.</param>
/// <param name="Status">Updated runtime status after setup.</param>
public sealed record RuntimeSetupResponse(
    bool RuntimeInstalled,
    bool ModelInstalled,
    string Message,
    RuntimeStatusDto Status);
