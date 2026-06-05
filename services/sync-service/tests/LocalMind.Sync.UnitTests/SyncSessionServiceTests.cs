namespace LocalMind.Sync.UnitTests;

using LocalMind.Sync.Application.Abstractions;
using LocalMind.Sync.Application.Sessions;
using LocalMind.Sync.Contracts.Sessions;
using LocalMind.Sync.Domain.Devices;
using LocalMind.Sync.Domain.Sessions;
using Xunit;

public sealed class SyncSessionServiceTests
{
    [Fact]
    public async Task CreateSessionRequiresRegisteredDevice()
    {
        SyncSessionService service = new(new Devices(), new Sessions(), new Clock());

        var result = await service.CreateAsync(new CreateSyncSessionRequest(Guid.NewGuid()), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("DEVICE_NOT_FOUND", result.Error?.Code);
    }

    private sealed class Clock : IClock
    {
        public DateTimeOffset UtcNow => DateTimeOffset.Parse("2026-06-05T00:00:00Z");
    }

    private sealed class Devices : IDeviceRepository
    {
        public Task<Device?> FindByIdAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult<Device?>(null);

        public Task<Device> SaveAsync(Device device, CancellationToken cancellationToken) => Task.FromResult(device);
    }

    private sealed class Sessions : ISyncSessionRepository
    {
        public Task<SyncSession?> FindByIdAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult<SyncSession?>(null);

        public Task<SyncSession> SaveAsync(SyncSession session, CancellationToken cancellationToken) => Task.FromResult(session);
    }
}
