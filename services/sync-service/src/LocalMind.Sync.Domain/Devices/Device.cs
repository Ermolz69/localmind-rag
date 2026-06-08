namespace LocalMind.Sync.Domain.Devices;

using LocalMind.Sync.Domain.Common;

public sealed class Device : Entity
{
    private Device(
        Guid id,
        string name,
        DevicePlatform platform,
        string clientVersion,
        string publicKey,
        DateTimeOffset lastSeenAt,
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt)
        : base(id, createdAt, updatedAt)
    {
        Name = name;
        Platform = platform;
        ClientVersion = clientVersion;
        PublicKey = publicKey;
        LastSeenAt = lastSeenAt;
    }

    public string Name { get; }

    public DevicePlatform Platform { get; }

    public string ClientVersion { get; }

    public string PublicKey { get; }

    public DateTimeOffset LastSeenAt { get; }

    public static Device Register(
        string name,
        DevicePlatform platform,
        string clientVersion,
        string publicKey,
        DateTimeOffset now)
    {
        return new Device(Guid.NewGuid(), name.Trim(), platform, clientVersion.Trim(), publicKey.Trim(), now, now, now);
    }

    public static Device Restore(
        Guid id,
        string name,
        DevicePlatform platform,
        string clientVersion,
        string publicKey,
        DateTimeOffset lastSeenAt,
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt)
    {
        return new Device(id, name, platform, clientVersion, publicKey, lastSeenAt, createdAt, updatedAt);
    }

    public Device Seen(DateTimeOffset now)
    {
        return new Device(Id, Name, Platform, ClientVersion, PublicKey, now, CreatedAt, now);
    }
}
