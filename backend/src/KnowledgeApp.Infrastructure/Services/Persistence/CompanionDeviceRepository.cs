using KnowledgeApp.Application.Companion;
using KnowledgeApp.Domain.Entities;
using KnowledgeApp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeApp.Infrastructure.Services.Persistence;

public sealed class CompanionDeviceRepository(AppDbContext dbContext) : ICompanionDeviceRepository
{
    public async Task AddAsync(CompanionDevice device, CancellationToken cancellationToken = default)
    {
        dbContext.CompanionDevices.Add(device);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<CompanionDevice>> ListAsync(CancellationToken cancellationToken = default)
    {
        // SQLite can't ORDER BY a DateTimeOffset, and the trusted-device list is tiny,
        // so order on the client.
        List<CompanionDevice> devices = await dbContext.CompanionDevices.ToListAsync(cancellationToken);
        return devices.OrderBy(device => device.CreatedAt).ToList();
    }

    public Task<CompanionDevice?> FindByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default)
    {
        return dbContext.CompanionDevices
            .FirstOrDefaultAsync(device => device.TokenHash == tokenHash, cancellationToken);
    }

    public Task<CompanionDevice?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return dbContext.CompanionDevices
            .FirstOrDefaultAsync(device => device.Id == id, cancellationToken);
    }

    public async Task<bool> RemoveAsync(Guid id, CancellationToken cancellationToken = default)
    {
        int removed = await dbContext.CompanionDevices
            .Where(device => device.Id == id)
            .ExecuteDeleteAsync(cancellationToken);

        return removed > 0;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }
}
