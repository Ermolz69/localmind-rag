namespace LocalMind.Sync.Application.Devices;

using LocalMind.Sync.Application.Abstractions;
using LocalMind.Sync.Application.Common;
using LocalMind.Sync.Application.Mappers;
using LocalMind.Sync.Contracts.Devices;
using LocalMind.Sync.Domain.Devices;

public sealed class DeviceService
{
    private readonly IClock clock;
    private readonly IDeviceRepository devices;

    public DeviceService(IDeviceRepository devices, IClock clock)
    {
        this.devices = devices;
        this.clock = clock;
    }

    public async Task<Result<DeviceResponse>> RegisterAsync(RegisterDeviceRequest request, CancellationToken cancellationToken)
    {
        Dictionary<string, string> errors = Validate(request);
        if (errors.Count > 0)
        {
            return Result<DeviceResponse>.Failure(ApplicationError.Validation("Device registration is invalid", errors));
        }

        Enum.TryParse(request.Platform, ignoreCase: true, out DevicePlatform platform);
        Device device = Device.Register(request.Name, platform, request.ClientVersion, request.PublicKey, clock.UtcNow);
        Device saved = await devices.SaveAsync(device, cancellationToken);
        return Result<DeviceResponse>.Success(ContractMappers.ToResponse(saved));
    }

    public async Task<Result<DeviceResponse>> GetAsync(Guid deviceId, CancellationToken cancellationToken)
    {
        Device? device = await devices.FindByIdAsync(deviceId, cancellationToken);
        return device is null
            ? Result<DeviceResponse>.Failure(ApplicationError.NotFound("DEVICE_NOT_FOUND", "Device was not found"))
            : Result<DeviceResponse>.Success(ContractMappers.ToResponse(device));
    }

    private static Dictionary<string, string> Validate(RegisterDeviceRequest request)
    {
        Dictionary<string, string> errors = new(StringComparer.Ordinal);
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            errors["name"] = "Device name is required";
        }

        if (string.IsNullOrWhiteSpace(request.ClientVersion))
        {
            errors["clientVersion"] = "Client version is required";
        }

        if (string.IsNullOrWhiteSpace(request.PublicKey))
        {
            errors["publicKey"] = "Public key is required";
        }

        return errors;
    }
}
