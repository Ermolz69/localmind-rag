using KnowledgeApp.Application.Companion;
using KnowledgeApp.Contracts.Common;
using KnowledgeApp.Contracts.Companion;
using Microsoft.AspNetCore.Mvc;

namespace KnowledgeApp.LocalApi.Endpoints;

public static class CompanionEndpoints
{
    public static IEndpointRouteBuilder MapCompanionEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/companion/pairing", async (
            [FromServices] ICompanionPairingService service,
            HttpContext context,
            CancellationToken cancellationToken) =>
        {
            var result = await service.StartAsync(cancellationToken);
            return result.ToApiResult(context);
        })
        .WithName("StartCompanionPairing")
        .WithTags("Companion")
        .WithSummary("Start Companion Mode pairing.")
        .WithDescription("Starts a time-limited pairing session and returns the QR payload. Fails when Companion Mode is disabled.")
        .Produces<ApiResponse<CompanionPairingSessionDto>>();

        app.MapGet("/companion/pairing", (
            [FromServices] ICompanionPairingService service,
            HttpContext context) =>
        {
            CompanionPairingStatusDto status = service.GetStatus();
            return ApiResults.Ok(status, context);
        })
        .WithName("GetCompanionPairingStatus")
        .WithTags("Companion")
        .WithSummary("Get Companion Mode pairing status.")
        .WithDescription("Returns whether a pairing session is active and how long it remains valid.")
        .Produces<ApiResponse<CompanionPairingStatusDto>>();

        app.MapDelete("/companion/pairing", (
            [FromServices] ICompanionPairingService service,
            HttpContext context) =>
        {
            var result = service.Cancel();
            return result.ToApiResult(context);
        })
        .WithName("CancelCompanionPairing")
        .WithTags("Companion")
        .WithSummary("Cancel Companion Mode pairing.")
        .WithDescription("Cancels any active pairing session.")
        .Produces<ApiResponse<object>>();

        app.MapPost("/companion/pairing/confirm", (
            [FromBody] ConfirmCompanionPairingRequest request,
            [FromServices] ICompanionPairingService service,
            HttpContext context) =>
        {
            var result = service.Confirm(request);
            return result.ToApiResult(context);
        })
        .WithName("ConfirmCompanionPairing")
        .WithTags("Companion")
        .WithSummary("Confirm Companion Mode pairing.")
        .WithDescription("Completes a pairing session and registers the calling device as trusted.")
        .Produces<ApiResponse<CompanionDeviceDto>>();

        app.MapGet("/companion/devices", (
            [FromServices] ICompanionPairingService service,
            HttpContext context) =>
        {
            CompanionDevicesResponse devices = service.GetDevices();
            return ApiResults.Ok(devices, context);
        })
        .WithName("GetCompanionDevices")
        .WithTags("Companion")
        .WithSummary("List trusted Companion Mode devices.")
        .WithDescription("Returns the phones currently paired as trusted devices.")
        .Produces<ApiResponse<CompanionDevicesResponse>>();

        app.MapDelete("/companion/devices/{deviceId:guid}", (
            Guid deviceId,
            [FromServices] ICompanionPairingService service,
            HttpContext context) =>
        {
            var result = service.RevokeDevice(deviceId);
            return result.ToApiResult(context);
        })
        .WithName("RevokeCompanionDevice")
        .WithTags("Companion")
        .WithSummary("Disconnect a trusted device.")
        .WithDescription("Removes a phone from the trusted Companion Mode devices.")
        .Produces<ApiResponse<object>>();

        return app;
    }
}
