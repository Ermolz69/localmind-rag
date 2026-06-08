using System.Net;
using KnowledgeApp.Contracts.WatchedFolders;

namespace KnowledgeApp.IntegrationTests;

public sealed class WatchedFolderEndpointsTests : IClassFixture<LocalApiTestFactory>
{
    private readonly LocalApiTestFactory factory;

    public WatchedFolderEndpointsTests(LocalApiTestFactory factory)
    {
        this.factory = factory;
    }

    [Fact]
    public async Task GetWatchedFolderStatus_Should_Return_Status_Response()
    {
        using HttpClient client = factory.CreateClient();

        WatchedFolderStatusResponse? status =
            await client.GetApiDataAsync<WatchedFolderStatusResponse>("/api/v1/watched-folders/status");

        Assert.NotNull(status);
        Assert.InRange(status.DebounceMilliseconds, 250, 60000);
        Assert.Equal("MarkDeleted", status.DeletePolicy);
        Assert.NotNull(status.Folders);
    }

    [Fact]
    public async Task GetWatchedFolderStatus_Should_Not_Return_NotFound()
    {
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage response = await client.GetAsync("/api/v1/watched-folders/status");

        Assert.NotEqual(HttpStatusCode.NotFound, response.StatusCode);
    }
}
