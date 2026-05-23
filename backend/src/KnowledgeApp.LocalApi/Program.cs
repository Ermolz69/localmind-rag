using KnowledgeApp.Bootstrap;
using KnowledgeApp.LocalApi.Endpoints;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.AddKnowledgeAppBootstrap();
builder.Services.AddOpenApi("v1", options =>
{
    options.AddDocumentTransformer((document, _, _) =>
    {
        document.Info.Title = "LocalMind Local API";
        document.Info.Version = "v1";
        document.Info.Description = "Local desktop backend for documents, notes, semantic search, RAG chat, runtime diagnostics, and settings.";
        return Task.CompletedTask;
    });
});

WebApplication app = builder.Build();

app.UseKnowledgeAppBootstrap();

app.MapOpenApi("/openapi/{documentName}.json");

app.MapHealthEndpoints();
app.MapDiagnosticsEndpoints();
app.MapRuntimeEndpoints();
app.MapBucketEndpoints();
app.MapDocumentEndpoints();
app.MapIngestionEndpoints();
app.MapNoteEndpoints();
app.MapChatEndpoints();
app.MapSearchEndpoints();
app.MapSettingsEndpoints();
app.MapSyncEndpoints();

app.Run();

public partial class Program;
