using System.Net;
using System.Net.Sockets;
using KnowledgeApp.Bootstrap;
using KnowledgeApp.LocalApi;
using KnowledgeApp.LocalApi.Endpoints;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

ConfigureLocalApiUrls(builder);

builder.AddKnowledgeAppBootstrap();

builder.Services.AddOpenApi(ApiVersions.V1DocumentName, options =>
{
    options.AddDocumentTransformer((document, _, _) =>
    {
        document.Info.Title = "LocalMind Local API";
        document.Info.Version = ApiVersions.V1DocumentName;
        document.Info.Description = "Local desktop backend for documents, notes, semantic search, RAG chat, runtime diagnostics, and settings.";

        return Task.CompletedTask;
    });
});

builder.Services.AddHostedService<KnowledgeApp.LocalApi.Services.SidecarPortWriter>();

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
apiV1.MapWatchedFolderEndpoints();
apiV1.MapSyncEndpoints();

app.Run();

static void ConfigureLocalApiUrls(WebApplicationBuilder builder)
{
    string? supervisedPort = Environment.GetEnvironmentVariable("LOCALMIND_LOCAL_API_PORT");

    if (ushort.TryParse(supervisedPort, out ushort port) && port > 0)
    {
        builder.WebHost.UseUrls($"http://127.0.0.1:{port}");
        return;
    }

    if (HasConfiguredUrls(builder))
    {
        return;
    }

    int fallbackPort = 49321;

    if (!IsPortAvailable(fallbackPort))
    {
        fallbackPort = 0;
    }

    builder.WebHost.UseUrls($"http://127.0.0.1:{fallbackPort}");
}

static bool HasConfiguredUrls(WebApplicationBuilder builder)
{
    return !string.IsNullOrWhiteSpace(builder.Configuration["urls"])
        || !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("ASPNETCORE_URLS"))
        || !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("DOTNET_URLS"));
}

static bool IsPortAvailable(int port)
{
    try
    {
        using Socket socket = new Socket(
            AddressFamily.InterNetwork,
            SocketType.Stream,
            ProtocolType.Tcp);

        socket.Bind(new IPEndPoint(IPAddress.Loopback, port));

        return true;
    }
    catch
    {
        return false;
    }
}

public partial class Program;
