namespace KnowledgeApp.Contracts.Runtime;

/// <summary>Runtime diagnostics for LocalApi and AI services.</summary>
public sealed record DiagnosticsRuntimeDto(
    DiagnosticsHealthStatus Status,
    string RuntimeMode,
    string LocalApiVersion,
    RuntimeStatusDto AiRuntimeStatus);

