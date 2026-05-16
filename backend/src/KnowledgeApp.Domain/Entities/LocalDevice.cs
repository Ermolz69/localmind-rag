using KnowledgeApp.Domain.Common;
using KnowledgeApp.Domain.Enums;

namespace KnowledgeApp.Domain.Entities;

public sealed class LocalDevice : Entity
{
    public string DeviceKey { get; set; } = string.Empty;
    public string Name { get; set; } = Environment.MachineName;
}
