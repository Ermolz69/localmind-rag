using KnowledgeApp.Bootstrap;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.AddKnowledgeAppBootstrap();
builder.Services.AddOpenApi();

WebApplication app = builder.Build();

app.UseKnowledgeAppBootstrap();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapGet("/api/health", () => Results.Ok(new { status = "OK", app = "localmind-sync" }));
app.MapPost("/api/auth/register", () => Results.Accepted());
app.MapPost("/api/auth/login", () => Results.Accepted());
app.MapPost("/api/devices/register", () => Results.Accepted());
app.MapGet("/api/sync/manifest", () => Results.Ok(Array.Empty<object>()));
app.MapPost("/api/sync/push", () => Results.Accepted());
app.MapPost("/api/sync/pull", () => Results.Ok(Array.Empty<object>()));
app.MapPost("/api/files/upload", () => Results.Accepted()).DisableAntiforgery();
app.MapGet("/api/files/{id:guid}/download", (Guid id) => Results.NotFound());

app.Run();

public partial class Program;
