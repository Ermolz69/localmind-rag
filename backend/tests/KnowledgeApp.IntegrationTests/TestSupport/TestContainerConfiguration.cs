using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace KnowledgeApp.IntegrationTests.TestSupport;

public static class TestContainerConfiguration
{
    public static IWebHostBuilder UseAiRuntimeBaseUrl(this IWebHostBuilder builder, string baseUrl)
    {
        return builder.ConfigureAppConfiguration((_, configuration) =>
        {
            Dictionary<string, string?> settings = new()
            {
                ["Ai:BaseUrl"] = baseUrl,
                ["Ai:Provider"] = "Stub",
                ["Ai:AutoStartRuntime"] = "false",
            };

            configuration.AddInMemoryCollection(settings);
        });
    }
}
