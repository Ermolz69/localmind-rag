using KnowledgeApp.Bootstrap;
using KnowledgeApp.LocalApi.Endpoints;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.AddKnowledgeAppBootstrap();
builder.Services.AddOpenApi();

WebApplication app = builder.Build();

app.UseKnowledgeAppBootstrap();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

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
