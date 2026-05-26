using KnowledgeApp.Bootstrap;
using KnowledgeApp.LocalApi;
using KnowledgeApp.LocalApi.Endpoints;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.AddKnowledgeAppBootstrap();

builder.Services.AddOpenApi(ApiVersions.V1DocumentName, options =>
{
    options.AddDocumentTransformer((document, _, _) =>
    {
        document.Info.Title = "LocalMind Local API";
        document.Info.Version = ApiVersions.V1DocumentName;
        document.Info.Description =
            "Local desktop backend for documents, notes, semantic search, RAG chat, runtime diagnostics, and settings.";

        return Task.CompletedTask;
    });
});

WebApplication app = builder.Build();

app.UseKnowledgeAppBootstrap();

app.MapOpenApi("/openapi/{documentName}.json");

RouteGroupBuilder apiV1 = app.MapGroup(ApiVersions.V1Prefix);

apiV1.MapHealthEndpoints();
apiV1.MapDiagnosticsEndpoints();
apiV1.MapRuntimeEndpoints();
apiV1.MapBucketEndpoints();
apiV1.MapDocumentEndpoints();
apiV1.MapIngestionEndpoints();
apiV1.MapNoteEndpoints();
apiV1.MapChatEndpoints();
apiV1.MapSearchEndpoints();
apiV1.MapSettingsEndpoints();
apiV1.MapSyncEndpoints();

app.Run();

public partial class Program;
