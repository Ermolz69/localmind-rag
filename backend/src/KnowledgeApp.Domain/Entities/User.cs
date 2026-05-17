using KnowledgeApp.Domain.Common;
using KnowledgeApp.Domain.Enums;

namespace KnowledgeApp.Domain.Entities;

public sealed class User : Entity
{
    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
}
