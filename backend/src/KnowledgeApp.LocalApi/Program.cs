using KnowledgeApp.Application.Abstractions;
using KnowledgeApp.Application.Documents;
using KnowledgeApp.Bootstrap;
using KnowledgeApp.Contracts.Rag;
using KnowledgeApp.Contracts.Runtime;
using KnowledgeApp.Domain.Entities;
using KnowledgeApp.Domain.Enums;
using KnowledgeApp.Infrastructure.Persistence;
using KnowledgeApp.Infrastructure.Options;
using KnowledgeApp.LocalApi;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

builder.AddKnowledgeAppBootstrap();
builder.Services.AddOpenApi();

var app = builder.Build();

app.UseKnowledgeAppBootstrap();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapGet("/api/health", () => Results.Ok(new { status = "OK", app = "localmind" }))
    .WithName("Health");

app.MapGet("/api/diagnostics",
    async (
        IAppPathProvider paths,
        IOptions<LocalRuntimeOptions> runtimeOptions,
        IAiRuntimeManager aiRuntimeManager,
        AppDbContext db,
        CancellationToken cancellationToken) =>
    {
        RuntimeStatusDto aiRuntimeStatus;

        try
        {
            aiRuntimeStatus = await aiRuntimeManager.GetStatusAsync(cancellationToken);
        }
        catch
        {
            aiRuntimeStatus = new RuntimeStatusDto(
                LocalApiReady: true,
                AiRuntimeStatus: "Missing",
                ModelsAvailable: false,
                OfflineMode: true);
        }

        var pendingIngestionJobsCount = await db.IngestionJobs.CountAsync(
            job => job.Status == IngestionJobStatus.Queued
                || job.Status == IngestionJobStatus.Running,
            cancellationToken);

        var localApiVersion =
            typeof(Program).Assembly
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                ?.InformationalVersion
            ?? typeof(Program).Assembly.GetName().Version?.ToString()
            ?? "unknown";

        var runtimeMode = runtimeOptions.Value.Portable ? "portable" : "dev";

        return Results.Ok(
            new DiagnosticsDto(
                Paths: new DiagnosticsPathsDto(
                    DatabasePath: paths.DatabasePath,
                    FilesPath: paths.FilesDirectory,
                    IndexPath: paths.IndexDirectory,
                    LogsPath: paths.LogsDirectory),
                RuntimeMode: runtimeMode,
                LocalApiVersion: localApiVersion,
                AiRuntimeStatus: aiRuntimeStatus,
                PendingIngestionJobsCount: pendingIngestionJobsCount));
    })
    .WithName("Diagnostics");

app.MapGet("/api/runtime/status", async (IAiRuntimeManager runtime, CancellationToken cancellationToken) =>
    Results.Ok(await runtime.GetStatusAsync(cancellationToken)));

app.MapPost("/api/runtime/ai/start", async (IAiRuntimeManager runtime, CancellationToken cancellationToken) =>
{
    await runtime.StartAsync(cancellationToken);
    return Results.Accepted();
});

app.MapGet("/api/runtime/models", async (IAiModelRegistry registry, CancellationToken cancellationToken) =>
    Results.Ok(await registry.ListModelsAsync(cancellationToken)));

app.MapGet("/api/buckets", async (AppDbContext db, CancellationToken cancellationToken) =>
    Results.Ok(await db.Buckets.OrderBy(x => x.Name).ToArrayAsync(cancellationToken)));

app.MapPost("/api/buckets", async (Bucket bucket, AppDbContext db, CancellationToken cancellationToken) =>
{
    db.Buckets.Add(bucket);
    await db.SaveChangesAsync(cancellationToken);
    return Results.Created($"/api/buckets/{bucket.Id}", bucket);
});

app.MapPut("/api/buckets/{id:guid}", async Task<Results<NoContent, NotFound>> (Guid id, Bucket request, AppDbContext db, CancellationToken cancellationToken) =>
{
    var bucket = await db.Buckets.FindAsync([id], cancellationToken);
    if (bucket is null)
    {
        return TypedResults.NotFound();
    }

    bucket.Name = request.Name;
    bucket.Description = request.Description;
    bucket.UpdatedAt = DateTimeOffset.UtcNow;
    await db.SaveChangesAsync(cancellationToken);
    return TypedResults.NoContent();
});

app.MapDelete("/api/buckets/{id:guid}", async Task<Results<NoContent, NotFound>> (Guid id, AppDbContext db, CancellationToken cancellationToken) =>
{
    var bucket = await db.Buckets.FindAsync([id], cancellationToken);
    if (bucket is null)
    {
        return TypedResults.NotFound();
    }

    db.Buckets.Remove(bucket);
    await db.SaveChangesAsync(cancellationToken);
    return TypedResults.NoContent();
});

app.MapGet("/api/documents", async (Guid? bucketId, GetDocumentsHandler handler, CancellationToken cancellationToken) =>
    Results.Ok(await handler.HandleAsync(new GetDocumentsQuery(bucketId), cancellationToken)));

app.MapPost("/api/documents/upload", async (IFormFile file, Guid? bucketId, UploadDocumentHandler handler, CancellationToken cancellationToken) =>
{
    await using var stream = file.OpenReadStream();
    var response = await handler.HandleAsync(
        new UploadDocumentCommand(stream, file.FileName, file.ContentType, file.Length, bucketId),
        cancellationToken);

    return Results.Created($"/api/documents/{response.DocumentId}", response);
}).DisableAntiforgery();

app.MapGet("/api/documents/{id:guid}", async (Guid id, GetDocumentByIdHandler handler, CancellationToken cancellationToken) =>
{
    var document = await handler.HandleAsync(new GetDocumentByIdQuery(id), cancellationToken);
    return document is null ? Results.NotFound() : Results.Ok(document);
});

app.MapDelete("/api/documents/{id:guid}", async Task<Results<NoContent, NotFound>> (Guid id, AppDbContext db, CancellationToken cancellationToken) =>
{
    var document = await db.Documents.FindAsync([id], cancellationToken);
    if (document is null)
    {
        return TypedResults.NotFound();
    }

    document.Status = DocumentStatus.Deleted;
    document.SyncStatus = SyncStatus.DeletedLocal;
    await db.SaveChangesAsync(cancellationToken);
    return TypedResults.NoContent();
});

app.MapPost("/api/documents/{id:guid}/reindex", async (Guid id, AppDbContext db, CancellationToken cancellationToken) =>
{
    var document = await db.Documents.FindAsync([id], cancellationToken);
    if (document is null)
    {
        return Results.NotFound();
    }

    db.IngestionJobs.Add(new IngestionJob { DocumentId = id });
    await db.SaveChangesAsync(cancellationToken);
    return Results.Accepted();
});

app.MapPost("/api/ingestion/jobs/{id:guid}/process", async (Guid id, IIngestionJobProcessor processor, AppDbContext db, CancellationToken cancellationToken) =>
{
    var exists = await db.IngestionJobs.AnyAsync(x => x.Id == id, cancellationToken);
    if (!exists)
    {
        return Results.NotFound();
    }

    await processor.ProcessAsync(id, cancellationToken);
    return Results.Accepted();
});

app.MapGet("/api/notes", async (AppDbContext db, CancellationToken cancellationToken) => Results.Ok(await db.Notes.ToArrayAsync(cancellationToken)));
app.MapPost("/api/notes", async (Note note, AppDbContext db, CancellationToken cancellationToken) =>
{
    db.Notes.Add(note);
    await db.SaveChangesAsync(cancellationToken);
    return Results.Created($"/api/notes/{note.Id}", note);
});
app.MapPut("/api/notes/{id:guid}", async Task<Results<NoContent, NotFound>> (Guid id, Note request, AppDbContext db, CancellationToken cancellationToken) =>
{
    var note = await db.Notes.FindAsync([id], cancellationToken);
    if (note is null)
    {
        return TypedResults.NotFound();
    }

    note.Title = request.Title;
    note.Markdown = request.Markdown;
    await db.SaveChangesAsync(cancellationToken);
    return TypedResults.NoContent();
});
app.MapDelete("/api/notes/{id:guid}", async Task<Results<NoContent, NotFound>> (Guid id, AppDbContext db, CancellationToken cancellationToken) =>
{
    var note = await db.Notes.FindAsync([id], cancellationToken);
    if (note is null)
    {
        return TypedResults.NotFound();
    }

    db.Notes.Remove(note);
    await db.SaveChangesAsync(cancellationToken);
    return TypedResults.NoContent();
});

app.MapGet("/api/chats", async (AppDbContext db, CancellationToken cancellationToken) => Results.Ok(await db.Conversations.ToArrayAsync(cancellationToken)));
app.MapPost("/api/chats", async (Conversation conversation, AppDbContext db, CancellationToken cancellationToken) =>
{
    db.Conversations.Add(conversation);
    await db.SaveChangesAsync(cancellationToken);
    return Results.Created($"/api/chats/{conversation.Id}", conversation);
});
app.MapPost("/api/chats/{id:guid}/messages", async (Guid id, ChatMessageRequest request, IRagAnswerGenerator rag, AppDbContext db, CancellationToken cancellationToken) =>
{
    db.ChatMessages.Add(new ChatMessage { ConversationId = id, Role = ChatRole.User, Content = request.Content });
    var answer = await rag.AnswerAsync(id, request.Content, cancellationToken);
    db.ChatMessages.Add(new ChatMessage { ConversationId = id, Role = ChatRole.Assistant, Content = answer.Answer });
    await db.SaveChangesAsync(cancellationToken);
    return Results.Ok(answer);
});

app.MapPost("/api/search/semantic", async (SemanticSearchRequest request, IRagContextBuilder rag, CancellationToken cancellationToken) =>
    Results.Ok(await rag.BuildAsync(request.Query, cancellationToken)));

app.MapGet("/api/settings", SettingsApi.GetAsync)
    .WithName("GetSettings");
app.MapPut("/api/settings", SettingsApi.PutAsync)
    .WithName("UpdateSettings");

app.MapGet("/api/sync/status", async (ISyncService sync, CancellationToken cancellationToken) => Results.Ok(await sync.GetStatusAsync(cancellationToken)));
app.MapPost("/api/sync/run", async (ISyncService sync, CancellationToken cancellationToken) =>
{
    await sync.RunAsync(cancellationToken);
    return Results.Accepted();
});
app.MapPost("/api/sync/login", () => Results.Problem("Remote sync auth is not implemented in the skeleton.", statusCode: StatusCodes.Status501NotImplemented));
app.MapPost("/api/sync/logout", () => Results.NoContent());

app.Run();

public partial class Program;
