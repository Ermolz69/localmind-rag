namespace LocalMind.Sync.Api.Endpoints;

using LocalMind.Sync.Api.Web;
using LocalMind.Sync.Application.Devices;
using LocalMind.Sync.Contracts.Devices;

public static class DeviceEndpoints
{
    public static RouteGroupBuilder MapDeviceEndpoints(this RouteGroupBuilder api)
    {
        api.MapPost("/devices/register", async (
                RegisterDeviceRequest request,
                DeviceService service,
                HttpContext context,
                CancellationToken cancellationToken) =>
            EndpointResults.From(await service.RegisterAsync(request, cancellationToken), context, StatusCodes.Status201Created));

        api.MapGet("/devices/{deviceId:guid}", async (
                Guid deviceId,
                DeviceService service,
                HttpContext context,
                CancellationToken cancellationToken) =>
            EndpointResults.From(await service.GetAsync(deviceId, cancellationToken), context));

        return api;
    }
}
