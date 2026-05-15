namespace KnowledgeApp.Contracts.Runtime;

public sealed record DiagnosticsRuntimeDto(
    string RuntimeMode,
    string LocalApiVersion,
    RuntimeStatusDto AiRuntimeStatus);


