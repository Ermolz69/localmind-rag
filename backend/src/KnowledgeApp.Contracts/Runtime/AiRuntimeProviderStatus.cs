namespace KnowledgeApp.Contracts.Runtime;

/// <summary>Stable provider status values for runtime diagnostics.</summary>
public static class AiRuntimeProviderStatus
{
    public const string Missing = "missing";
    public const string Stopped = "stopped";
    public const string Starting = "starting";
    public const string Running = "running";
    public const string Degraded = "degraded";
    public const string Failed = "failed";
}
