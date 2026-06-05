namespace LocalMind.Sync.UnitTests;

using LocalMind.Sync.Application.Abstractions;
using LocalMind.Sync.Application.Devices;
using LocalMind.Sync.Contracts.Devices;
using LocalMind.Sync.Domain.Devices;
using Xunit;

public sealed class DeviceServiceTests
{
    [Fact]
    public async Task RegisterRejectsMissingPublicKey()
    {
        DeviceService service = new(new InMemoryDeviceRepository(), new FixedClock());

        var result = await service.RegisterAsync(new RegisterDeviceRequest("Desktop", "Windows", "1.0.0", ""), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("VALIDATION_FAILED", result.Error?.Code);
    }

    [Fact]
    public async Task RegisterCreatesDevice()
    {
        DeviceService service = new(new InMemoryDeviceRepository(), new FixedClock());

        var result = await service.RegisterAsync(new RegisterDeviceRequest("Desktop", "Windows", "1.0.0", "public-key"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Desktop", result.Value?.Name);
        Assert.Equal("Windows", result.Value?.Platform);
    }

    private sealed class FixedClock : IClock
    {
        public DateTimeOffset UtcNow => DateTimeOffset.Parse("2026-06-05T00:00:00Z");
    }

    private sealed class InMemoryDeviceRepository : IDeviceRepository
    {
        private readonly Dictionary<Guid, Device> devices = [];

        public Task<Device> SaveAsync(Device device, CancellationToken cancellationToken)
        {
            devices[device.Id] = device;
            return Task.FromResult(device);
        }

        public Task<Device?> FindByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            devices.TryGetValue(id, out Device? device);
            return Task.FromResult(device);
        }
    }
}
