namespace KnowledgeApp.Contracts.Companion;

/// <summary>A phone that has been paired as a trusted Companion Mode device.</summary>
/// <param name="Id">Stable device identifier.</param>
/// <param name="Name">Human-friendly device name, e.g. "Redmi Note".</param>
/// <param name="Platform">Client platform, e.g. "Chrome".</param>
/// <param name="CreatedAt">When the device was first paired.</param>
/// <param name="LastSeenAt">When the device last connected.</param>
/// <param name="Permissions">What this device is allowed to do.</param>
public sealed record CompanionDeviceDto(
    Guid Id,
    string Name,
    string Platform,
    DateTimeOffset CreatedAt,
    DateTimeOffset LastSeenAt,
    CompanionDevicePermissionsDto Permissions);
