using KnowledgeApp.Application.Abstractions;
using KnowledgeApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeApp.Application.Common.Devices;

public sealed class LocalDeviceResolver(
    IAppDbContext dbContext,
    IDateTimeProvider dateTimeProvider,
    ILocalDeviceIdentityProvider identityProvider) : ILocalDeviceResolver
{
    public async Task<Guid> ResolveCurrentDeviceIdAsync(CancellationToken cancellationToken = default)
    {
        LocalDeviceIdentity identity = identityProvider.GetIdentity();
        LocalDevice? device = await dbContext.LocalDevices
            .FirstOrDefaultAsync(item => item.DeviceKey == identity.DeviceKey, cancellationToken);

        if (device is not null)
        {
            if (device.Name != identity.Name)
            {
                device.Name = identity.Name;
                device.UpdatedAt = dateTimeProvider.UtcNow;
                await dbContext.SaveChangesAsync(cancellationToken);
            }

            return device.Id;
        }

        LocalDevice createdDevice = new()
        {
            DeviceKey = identity.DeviceKey,
            Name = identity.Name,
        };
        dbContext.LocalDevices.Add(createdDevice);
        await dbContext.SaveChangesAsync(cancellationToken);

        return createdDevice.Id;
    }
}
