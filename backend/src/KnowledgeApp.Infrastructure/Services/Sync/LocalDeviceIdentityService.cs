using KnowledgeApp.Application.Abstractions;
using KnowledgeApp.Application.Abstractions.Sync;
using KnowledgeApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeApp.Infrastructure.Services.Sync;

public sealed class LocalDeviceIdentityService(IAppDbContext dbContext) : ILocalDeviceIdentityService
{
    private Guid? _cachedDeviceId;

    public async Task<Guid> GetLocalDeviceIdAsync(CancellationToken cancellationToken = default)
    {
        if (_cachedDeviceId.HasValue)
        {
            return _cachedDeviceId.Value;
        }

        LocalDevice? device = await dbContext.LocalDevices.FirstOrDefaultAsync(cancellationToken);

        if (device is null)
        {
            device = new LocalDevice
            {
                DeviceKey = Guid.NewGuid().ToString(),
                Name = Environment.MachineName
            };

            dbContext.LocalDevices.Add(device);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        _cachedDeviceId = device.Id;
        return device.Id;
    }
}
