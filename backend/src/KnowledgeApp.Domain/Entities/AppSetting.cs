using KnowledgeApp.Domain.Common;
using KnowledgeApp.Domain.Enums;

namespace KnowledgeApp.Domain.Entities;

public sealed class AppSetting : Entity
{
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}
