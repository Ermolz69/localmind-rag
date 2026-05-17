using KnowledgeApp.Application.Abstractions;

namespace KnowledgeApp.UnitTests;

internal sealed class FakeLocalDeviceResolver(Guid? deviceId = null) : ILocalDeviceResolver
{
    public Guid DeviceId { get; } = deviceId ?? Guid.Parse("11111111-1111-1111-1111-111111111111");

    public Task<Guid> ResolveCurrentDeviceIdAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(DeviceId);
    }
}
