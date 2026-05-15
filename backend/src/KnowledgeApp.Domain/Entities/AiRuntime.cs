using KnowledgeApp.Domain.Common;
using KnowledgeApp.Domain.Enums;

namespace KnowledgeApp.Domain.Entities;

public sealed class AiRuntime : Entity
{
    public AiProviderType Provider { get; set; } = AiProviderType.LlamaCpp;
    public AiRuntimeStatus Status { get; set; } = AiRuntimeStatus.Unknown;
    public string BaseUrl { get; set; } = string.Empty;
}
