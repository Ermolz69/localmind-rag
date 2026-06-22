namespace KnowledgeApp.Contracts.Companion;

/// <summary>The list of trusted Companion Mode devices.</summary>
/// <param name="Devices">Currently trusted devices.</param>
public sealed record CompanionDevicesResponse(IReadOnlyList<CompanionDeviceDto> Devices);
