using System.Net;
using System.Net.Http.Json;
using KnowledgeApp.Application.Abstractions;
using KnowledgeApp.Application.Common.Diagnostics;
using KnowledgeApp.Contracts.Chats;
using KnowledgeApp.Contracts.Rag;
using KnowledgeApp.Contracts.Settings;
using KnowledgeApp.IntegrationTests.TestSupport;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace KnowledgeApp.IntegrationTests;

public sealed class ObservabilityApiTests
{
    [Fact]
    public async Task Health_Request_Should_Create_Request_Log_Entry()
    {
        using ObservabilityTestContext context = CreateContext();
        using HttpClient client = context.Factory.CreateClient();

        using HttpResponseMessage response = await client.GetAsync("/api/v1/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        string logs = await context.ReadLogsAsync("localmind*.log");

        Assert.Contains("HTTP GET /api/v1/health responded 200", logs, StringComparison.Ordinal);
        Assert.Empty(context.ReadLogsNow("http*.log"));
        Assert.Empty(context.ReadLogsNow("errors*.log"));
        Assert.Empty(context.ReadLogsNow("sql*.log"));
    }

    [Fact]
    public async Task Health_Request_Should_Create_Http_Log_Entry()
    {
        using ObservabilityTestContext context = CreateContext(
            useSeparateLogFiles: true,
            enableHttpLogs: true);
        using HttpClient client = context.Factory.CreateClient();

        using HttpResponseMessage response = await client.GetAsync("/api/v1/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        string logs = await context.ReadLogsAsync("http*.log");

        Assert.Contains("HTTP GET /api/v1/health responded 200", logs, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Database_Diagnostics_Should_Create_Sql_Log_Entry_When_Enabled()
    {
        using ObservabilityTestContext context = CreateContext(
            useSeparateLogFiles: true,
            enableSqlLogs: true);
        using HttpClient client = context.Factory.CreateClient();

        using HttpResponseMessage response = await client.GetAsync("/api/v1/diagnostics/database");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        string logs = await context.ReadLogsAsync("sql*.log");

        Assert.Contains("SQL SELECT", logs, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Failed_Request_Should_Create_Error_Log_With_Trace_Context()
    {
        using ObservabilityTestContext context = CreateContext(
            useSeparateLogFiles: true,
            enableErrorLogs: true);
        using HttpClient client = context.Factory.CreateClient();

        AppSettingsDto request = new(
            Appearance: new AppearanceSettingsDto("Broken"),
            Ai: new AiSettingsDto("Unknown", "", "", "", ""),
            RuntimePaths: new RuntimePathsSettingsDto("", "", "", "", ""),
            Sync: new SyncSettingsDto(false, false),
            Diagnostics: new DiagnosticsSettingsDto(Enabled: false),
            WatchedFolders: CreateDefaultWatchedFoldersSettings());

        using HttpResponseMessage response = await client.PutAsJsonAsync("/api/v1/settings", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        string logs = await context.ReadLogsAsync("errors*.log");

        Assert.Contains("HTTP PUT /api/v1/settings responded 400", logs, StringComparison.Ordinal);
        Assert.Contains("TraceId", logs, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Chat_Rag_Flow_Should_Create_Advanced_Diagnostic_Events()
    {
        using ObservabilityTestContext context = CreateContext(
            useSeparateLogFiles: true,
            enableDiagnosticEventLogs: true);
        using HttpClient client = context.Factory.CreateClient();

        ConversationDto conversation = await ApiScenarioHelpers.CreateConversationAsync(client);

        using HttpResponseMessage response = await client.PostAsJsonAsync(
            $"/api/v1/chats/{conversation.Id}/messages",
            new ChatMessageRequest("What local sources exist?"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        string events = await context.ReadLogsAsync("advanced-events*.ndjson");

        Assert.Contains("\"EventKind\":\"Diagnostic\"", events, StringComparison.Ordinal);
        Assert.Contains("build-context", events, StringComparison.Ordinal);
        Assert.Contains("answer-generated", events, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Failed_Semantic_Search_Should_Create_Diagnostic_Failure_Event()
    {
        using ObservabilityTestContext context = CreateContext(
            builder =>
                builder.ConfigureTestServices(services =>
                {
                    services.RemoveAll<IEmbeddingGenerator>();
                    services.AddSingleton<IEmbeddingGenerator, FailingEmbeddingGenerator>();
                }),
            useSeparateLogFiles: true,
            enableDiagnosticEventLogs: true);

        using HttpClient client = context.Factory.CreateClient();

        using HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/v1/search/semantic",
            new SemanticSearchRequest("diagnostic failure"));

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);

        string events = await context.ReadLogsAsync("advanced-events*.ndjson");

        Assert.Contains(DiagnosticNames.Steps.SemanticSearchStarted, events, StringComparison.Ordinal);
        Assert.Contains(DiagnosticNames.Steps.SemanticSearchFailed, events, StringComparison.Ordinal);
        Assert.Contains("Synthetic embedding failure.", events, StringComparison.Ordinal);
    }

    private static ObservabilityTestContext CreateContext(
        Action<IWebHostBuilder>? configure = null,
        bool useSeparateLogFiles = false,
        bool enableErrorLogs = true,
        bool enableHttpLogs = true,
        bool enableSqlLogs = false,
        bool enableDiagnosticEventLogs = false,
        bool enableDebugTrace = false)
    {
        string root = Path.Combine(Path.GetTempPath(), "localmind-observability", Guid.NewGuid().ToString("N"));
        string logsPath = Path.Combine(root, "logs");

        Directory.CreateDirectory(logsPath);

        LocalApiTestFactory baseFactory = new();

        WebApplicationFactory<Program> factory = baseFactory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, configuration) =>
            {
                Dictionary<string, string?> settings = new()
                {
                    ["Observability:Enabled"] = "true",
                    ["Observability:Mode"] = "Advanced",
                    ["Observability:LogsPath"] = logsPath,
                    ["Observability:MinimumLevel"] = "Information",
                    ["Observability:UseSeparateLogFiles"] = useSeparateLogFiles.ToString(),
                    ["Observability:EnableErrorLogs"] = enableErrorLogs.ToString(),
                    ["Observability:EnableHttpLogs"] = enableHttpLogs.ToString(),
                    ["Observability:EnableSqlLogs"] = enableSqlLogs.ToString(),
                    ["Observability:EnableDiagnosticEventLogs"] = enableDiagnosticEventLogs.ToString(),
                    ["Observability:EnableDebugTrace"] = enableDebugTrace.ToString(),
                };

                configuration.AddInMemoryCollection(settings);
            });

            configure?.Invoke(builder);
        });

        return new ObservabilityTestContext(root, logsPath, baseFactory, factory);
    }

    private static WatchedFoldersSettingsDto CreateDefaultWatchedFoldersSettings()
    {
        return new WatchedFoldersSettingsDto(
            Enabled: false,
            DebounceMilliseconds: 1000,
            DeletePolicy: "MarkDeleted",
            Folders: []);
    }

    private sealed class FailingEmbeddingGenerator : IEmbeddingGenerator
    {
        public string ModelName => "failing-test-model";

        public Task<float[]> GenerateAsync(string text, CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException("Synthetic embedding failure.");
        }
    }

    private sealed class ObservabilityTestContext(
        string root,
        string logsPath,
        LocalApiTestFactory baseFactory,
        WebApplicationFactory<Program> factory) : IDisposable
    {
        public WebApplicationFactory<Program> Factory => factory;

        public async Task<string> ReadLogsAsync(string pattern)
        {
            DateTimeOffset deadline = DateTimeOffset.UtcNow.AddSeconds(5);

            while (DateTimeOffset.UtcNow < deadline)
            {
                string content = string.Join(
                    Environment.NewLine,
                    Directory.EnumerateFiles(logsPath, pattern)
                        .Select(ReadLogFile));

                if (!string.IsNullOrWhiteSpace(content))
                {
                    return content;
                }

                await Task.Delay(100);
            }

            return string.Join(
                Environment.NewLine,
                Directory.Exists(logsPath)
                    ? Directory.EnumerateFiles(logsPath, pattern).Select(ReadLogFile)
                    : []);
        }

        public string ReadLogsNow(string pattern)
        {
            return string.Join(
                Environment.NewLine,
                Directory.Exists(logsPath)
                    ? Directory.EnumerateFiles(logsPath, pattern).Select(ReadLogFile)
                    : []);
        }

        private static string ReadLogFile(string path)
        {
            using FileStream stream = new(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
            using StreamReader reader = new(stream);

            return reader.ReadToEnd();
        }

        public void Dispose()
        {
            factory.Dispose();
            baseFactory.Dispose();

            if (Directory.Exists(root))
            {
                try
                {
                    Directory.Delete(root, recursive: true);
                }
                catch (IOException)
                {
                }
                catch (UnauthorizedAccessException)
                {
                }
            }
        }
    }
}
