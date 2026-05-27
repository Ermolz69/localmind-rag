using KnowledgeApp.Application.Abstractions;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace KnowledgeApp.RagEvaluationTests.TestSupport;

public sealed class RagEvaluationTestFactory : WebApplicationFactory<Program>
{
    private readonly string runtimeRoot = Path.Combine(
        Path.GetTempPath(),
        "localmind-rag-evaluation",
        Guid.NewGuid().ToString("N"));

    internal SemaphoreSlim SeedGate { get; } = new(1, 1);

    internal bool FixturesSeeded { get; set; }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            Dictionary<string, string?> settings = new()
            {
                ["LocalRuntime:DataPath"] =
                    Path.Combine(runtimeRoot, "runtime", "app", "data"),

                ["LocalRuntime:DatabasePath"] =
                    Path.Combine(
                        runtimeRoot,
                        "runtime",
                        "app",
                        "data",
                        "knowledge-app.db"),

                ["LocalRuntime:FilesPath"] =
                    Path.Combine(runtimeRoot, "runtime", "app", "files"),

                ["LocalRuntime:IndexPath"] =
                    Path.Combine(runtimeRoot, "runtime", "app", "indexes"),

                ["LocalRuntime:LogsPath"] =
                    Path.Combine(runtimeRoot, "runtime", "app", "logs"),

                ["Ai:Provider"] = "Stub",
                ["Ai:EmbeddingProvider"] = "Stub",
                ["Ai:EmbeddingModel"] = "controlled-fixture-embedding-v1",

                ["Ai:RuntimePath"] =
                    Path.Combine(
                        runtimeRoot,
                        "runtime",
                        "ai",
                        "bin",
                        "llama-server.exe"),

                ["Ai:ModelsPath"] =
                    Path.Combine(runtimeRoot, "runtime", "ai", "models"),

                ["Rag:MinimumSourceScore"] = "0.8",
                ["IngestionWorker:Enabled"] = "false",
            };

            configuration.AddInMemoryCollection(settings);
        });

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IEmbeddingGenerator>();

            services.AddSingleton<IEmbeddingGenerator, ControlledFixtureEmbeddingGenerator>();
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (!disposing)
        {
            return;
        }

        SeedGate.Dispose();

        if (!Directory.Exists(runtimeRoot))
        {
            return;
        }

        try
        {
            Directory.Delete(runtimeRoot, recursive: true);
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
}
