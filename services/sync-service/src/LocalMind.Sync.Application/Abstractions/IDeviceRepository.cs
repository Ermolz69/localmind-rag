namespace LocalMind.Sync.Application.Abstractions;

using LocalMind.Sync.Domain.Devices;

public interface IDeviceRepository
{
    Task<Device> SaveAsync(Device device, CancellationToken cancellationToken);

    Task<Device?> FindByIdAsync(Guid id, CancellationToken cancellationToken);
}
