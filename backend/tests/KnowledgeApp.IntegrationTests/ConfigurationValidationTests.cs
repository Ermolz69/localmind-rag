using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace KnowledgeApp.IntegrationTests;

public sealed class ConfigurationValidationTests
{
    [Theory]
    [InlineData(
        "Ai:Provider",
        "invalid-provider",
        "Ai:Provider must be one of: LlamaCpp or Stub.")]
    [InlineData(
        "Ai:BaseUrl",
        "not-a-url",
        "Ai:BaseUrl must be an absolute HTTP or HTTPS URL.")]
    [InlineData(
        "Ai:ChatModel",
        "",
        "Ai:ChatModel is required.")]
    [InlineData(
        "Ai:RuntimePath",
        "",
        "Ai:RuntimePath must be a valid non-empty file path.")]
    [InlineData(
        "Ai:EmbeddingModel",
        "",
        "Ai:EmbeddingModel is required.")]
    [InlineData(
        "Ai:ModelsPath",
        "",
        "Ai:ModelsPath must be a valid non-empty directory path.")]
    [InlineData(
        "LocalRuntime:DataPath",
        "",
        "LocalRuntime:DataPath must be a valid non-empty directory path.")]
    [InlineData(
        "LocalRuntime:DatabasePath",
        "",
        "LocalRuntime:DatabasePath must be a valid non-empty file path.")]
    [InlineData(
        "LocalRuntime:FilesPath",
        "",
        "LocalRuntime:FilesPath must be a valid non-empty directory path.")]
    [InlineData(
        "LocalRuntime:IndexPath",
        "",
        "LocalRuntime:IndexPath must be a valid non-empty directory path.")]
    [InlineData(
        "LocalRuntime:LogsPath",
        "",
        "LocalRuntime:LogsPath must be a valid non-empty directory path.")]
    public async Task Invalid_Critical_Configuration_Should_Fail_Application_Startup(
        string key,
        string value,
        string expectedMessage)
    {
        using LocalApiTestFactory baseFactory = new();

        using WebApplicationFactory<Program> invalidFactory =
            baseFactory.WithWebHostBuilder(builder =>
                builder.ConfigureAppConfiguration((_, configuration) =>
                {
                    configuration.AddInMemoryCollection(
                        new Dictionary<string, string?>
                        {
                            [key] = value,
                        });
                }));

        Exception exception = await Assert.ThrowsAnyAsync<Exception>(async () =>
        {
            using HttpClient client = invalidFactory.CreateClient();

            await client.GetAsync("/api/v1/health");
        });

        Assert.Contains(
            expectedMessage,
            exception.ToString(),
            StringComparison.Ordinal);
    }

    [Fact]
    public async Task Valid_Typed_Configuration_Should_Allow_Application_Startup()
    {
        using LocalApiTestFactory factory = new();
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage response =
            await client.GetAsync("/api/v1/health");

        response.EnsureSuccessStatusCode();
    }
}
