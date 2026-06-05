namespace LocalMind.Sync.Infrastructure.Mongo;

using LocalMind.Sync.Application.Abstractions;
using LocalMind.Sync.Domain.Devices;
using MongoDB.Driver;

public sealed class MongoDeviceRepository : IDeviceRepository
{
    private readonly MongoSyncContext context;

    public MongoDeviceRepository(MongoSyncContext context)
    {
        this.context = context;
    }

    public async Task<Device> SaveAsync(Device device, CancellationToken cancellationToken)
    {
        DeviceDocument document = MongoMappers.ToDocument(device);
        await context.Devices.ReplaceOneAsync(
            item => item.Id == device.Id,
            document,
            new ReplaceOptions { IsUpsert = true },
            cancellationToken);
        return device;
    }

    public async Task<Device?> FindByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        DeviceDocument? document = await context.Devices.Find(item => item.Id == id).FirstOrDefaultAsync(cancellationToken);
        return document is null ? null : MongoMappers.ToDevice(document);
    }
}
