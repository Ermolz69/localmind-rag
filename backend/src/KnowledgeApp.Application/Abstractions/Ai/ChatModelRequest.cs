using KnowledgeApp.Contracts.Rag;

namespace KnowledgeApp.Application.Abstractions;

public sealed record ChatModelRequest(
    string Question,
    string ContextText,
    IReadOnlyList<RagSourceDto> Sources,
    string? SystemPrompt = null,
    string? UserPrompt = null,
    double? Temperature = null);
