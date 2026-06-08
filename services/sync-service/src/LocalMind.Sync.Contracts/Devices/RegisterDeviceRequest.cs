namespace LocalMind.Sync.Contracts.Devices;

public sealed record RegisterDeviceRequest(string Name, string Platform, string ClientVersion, string PublicKey);
