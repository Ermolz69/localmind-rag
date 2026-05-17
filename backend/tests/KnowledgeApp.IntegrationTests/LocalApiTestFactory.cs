using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace KnowledgeApp.IntegrationTests;

public sealed class LocalApiTestFactory : WebApplicationFactory<Program>
{
    private readonly string runtimeRoot = Path.Combine(Path.GetTempPath(), "localmind-integration", Guid.NewGuid().ToString("N"));

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            Dictionary<string, string?> settings = new()
            {
                ["LocalRuntime:DataPath"] = Path.Combine(runtimeRoot, "runtime", "app", "data"),
                ["LocalRuntime:DatabasePath"] = Path.Combine(runtimeRoot, "runtime", "app", "data", "knowledge-app.db"),
                ["LocalRuntime:FilesPath"] = Path.Combine(runtimeRoot, "runtime", "app", "files"),
                ["LocalRuntime:IndexPath"] = Path.Combine(runtimeRoot, "runtime", "app", "indexes"),
                ["LocalRuntime:LogsPath"] = Path.Combine(runtimeRoot, "runtime", "app", "logs"),
            };

            configuration.AddInMemoryCollection(settings);
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing && Directory.Exists(runtimeRoot))
        {
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
}
