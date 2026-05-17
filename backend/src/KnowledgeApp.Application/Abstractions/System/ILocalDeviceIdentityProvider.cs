namespace KnowledgeApp.Application.Abstractions;

public sealed record LocalDeviceIdentity(string DeviceKey, string Name);

public interface ILocalDeviceIdentityProvider
{
    LocalDeviceIdentity GetIdentity();
}
