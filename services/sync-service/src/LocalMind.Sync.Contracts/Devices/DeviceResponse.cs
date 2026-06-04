namespace LocalMind.Sync.Contracts.Devices;

public sealed record DeviceResponse(
    Guid Id,
    string Name,
    string Platform,
    string ClientVersion,
    DateTimeOffset LastSeenAt,
    DateTimeOffset CreatedAt);
