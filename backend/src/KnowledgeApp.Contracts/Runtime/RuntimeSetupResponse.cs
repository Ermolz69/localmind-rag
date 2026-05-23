namespace KnowledgeApp.Contracts.Runtime;

public sealed record RuntimeSetupResponse(
    bool RuntimeInstalled,
    bool ModelInstalled,
    string Message,
    RuntimeStatusDto Status);
