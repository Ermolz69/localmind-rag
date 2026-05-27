using System.Net;

using KnowledgeApp.Contracts.Runtime;
using KnowledgeApp.IntegrationTests.TestSupport;

using Microsoft.AspNetCore.Mvc.Testing;

namespace KnowledgeApp.IntegrationTests;

public sealed class RuntimeProviderApiTests(LocalApiTestFactory factory)
    : IClassFixture<LocalApiTestFactory>
{
    [Fact]
    public async Task RuntimeProviders_Should_Return_Selected_Provider_And_Capabilities()
    {
        using HttpClient client = factory.CreateClient();

        RuntimeProviderListResponse providers =
            (await client.GetApiDataAsync<RuntimeProviderListResponse>(
                "/api/v1/runtime/providers"))!;

        Assert.Equal("stub", providers.SelectedProviderId);

        RuntimeProviderDto provider =
            Assert.Single(providers.Providers, provider => provider.Selected);

        Assert.Equal("stub", provider.Id);
        Assert.True(provider.Capabilities.SupportsEmbeddings);
        Assert.True(provider.Capabilities.SupportsChat);
    }
}

[Collection(ContainerizedExternalServicesCollection.Name)]
public sealed class RuntimeProviderTestcontainerTests(
    ContainerizedHttpServiceFixture httpService,
    LocalApiTestFactory factory) : IClassFixture<LocalApiTestFactory>
{
    [DockerFact]
    public async Task RuntimeStatus_Should_Use_Container_Injected_Runtime_BaseUrl()
    {
        using HttpClient sidecarClient = new();

        using HttpResponseMessage sidecarResponse =
            await sidecarClient.GetAsync(
                $"{httpService.BaseUrl}/health",
                CancellationToken.None);

        Assert.Equal(HttpStatusCode.OK, sidecarResponse.StatusCode);

        using WebApplicationFactory<Program> containerFactory =
            factory.WithWebHostBuilder(builder =>
                builder.UseAiRuntimeBaseUrl(httpService.BaseUrl));

        using HttpClient client = containerFactory.CreateClient();

        RuntimeStatusDto status =
            (await client.GetApiDataAsync<RuntimeStatusDto>(
                "/api/v1/runtime/status"))!;

        Assert.Equal("stub", status.ProviderId);
        Assert.Equal(httpService.BaseUrl, status.BaseUrl);
    }
}
