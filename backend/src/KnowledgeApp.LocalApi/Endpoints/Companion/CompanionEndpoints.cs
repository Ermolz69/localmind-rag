using KnowledgeApp.Application.Abstractions;
using KnowledgeApp.Application.Companion;
using KnowledgeApp.Application.Companion.Files;
using KnowledgeApp.Contracts.Common;
using KnowledgeApp.Contracts.Companion;
using KnowledgeApp.Contracts.Documents;
using Microsoft.AspNetCore.Mvc;

namespace KnowledgeApp.LocalApi.Endpoints;

public static class CompanionEndpoints
{
    public static IEndpointRouteBuilder MapCompanionEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/companion/info", (
            [FromServices] ICompanionPairingService service,
            HttpContext context) =>
        {
            CompanionInfoDto info = service.GetInfo();
            return ApiResults.Ok(info, context);
        })
        .WithName("GetCompanionInfo")
        .WithTags("Companion")
        .WithSummary("Get Companion Mode info.")
        .WithDescription("Returns lightweight info for the phone companion interface, such as the computer name.")
        .Produces<ApiResponse<CompanionInfoDto>>();

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

        app.MapGet("/companion/activity", (
            [FromQuery] int? limit,
            [FromServices] ICompanionActivityFeed activityFeed,
            HttpContext context) =>
        {
            var events = activityFeed.GetRecent(limit ?? 50);
            return ApiResults.Ok(new CompanionActivityResponse(events), context);
        })
        .WithName("GetCompanionActivity")
        .WithTags("Companion")
        .WithSummary("Recent companion activity.")
        .WithDescription("Returns recent activity events (ingestion, watched folders, devices) newest first.")
        .Produces<ApiResponse<CompanionActivityResponse>>();

        app.MapGet("/companion/files/roots", async (
            [FromServices] ICompanionFileService service,
            HttpContext context,
            CancellationToken cancellationToken) =>
        {
            CompanionRootsResponse roots = await service.GetRootsAsync(cancellationToken);
            return ApiResults.Ok(roots, context);
        })
        .WithName("GetCompanionFileRoots")
        .WithTags("Companion")
        .WithSummary("List allowed folders.")
        .WithDescription("Returns the folders the user allowed the phone to browse.")
        .Produces<ApiResponse<CompanionRootsResponse>>();

        app.MapGet("/companion/files/browse", async (
            [FromQuery] string path,
            [FromServices] ICompanionFileService service,
            HttpContext context,
            CancellationToken cancellationToken) =>
        {
            var result = await service.BrowseAsync(path, cancellationToken);
            return result.ToApiResult(context);
        })
        .WithName("BrowseCompanionFiles")
        .WithTags("Companion")
        .WithSummary("Browse an allowed folder.")
        .WithDescription("Lists subfolders and supported files inside an allowed folder.")
        .Produces<ApiResponse<CompanionBrowseResponse>>();

        app.MapPost("/companion/files/add", async (
            [FromBody] AddCompanionFileRequest request,
            [FromServices] ICompanionFileService service,
            HttpContext context,
            CancellationToken cancellationToken) =>
        {
            var result = await service.AddFileAsync(request.Path, cancellationToken);
            return result.ToApiResult(context);
        })
        .WithName("AddCompanionFile")
        .WithTags("Companion")
        .WithSummary("Add a file from an allowed folder.")
        .WithDescription("Adds a file from an allowed folder into LocalMind for indexing.")
        .Produces<ApiResponse<UploadDocumentResponse>>();

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

        app.MapPut("/companion/devices/{deviceId:guid}/permissions", (
            Guid deviceId,
            [FromBody] CompanionDevicePermissionsDto permissions,
            [FromServices] ICompanionPairingService service,
            HttpContext context) =>
        {
            var result = service.UpdateDevicePermissions(deviceId, permissions);
            return result.ToApiResult(context);
        })
        .WithName("UpdateCompanionDevicePermissions")
        .WithTags("Companion")
        .WithSummary("Update a trusted device's permissions.")
        .WithDescription("Sets what a trusted device is allowed to do.")
        .Produces<ApiResponse<object>>();

        return app;
    }
}
