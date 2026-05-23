using System.Net;
using System.Net.Http.Json;
using KnowledgeApp.Contracts.Buckets;
using KnowledgeApp.Contracts.Chats;
using KnowledgeApp.Contracts.Common;
using KnowledgeApp.IntegrationTests.TestSupport;
using Microsoft.AspNetCore.Mvc;

namespace KnowledgeApp.IntegrationTests;

public sealed class ApiPaginationTests
{
    [Fact]
    public async Task Chats_Should_Return_Next_Cursor_Page_Without_Duplicates()
    {
        using LocalApiTestFactory factory = new();
        using HttpClient client = factory.CreateClient();
        ConversationDto first = await ApiScenarioHelpers.CreateConversationAsync(client, "First chat");
        ConversationDto second = await ApiScenarioHelpers.CreateConversationAsync(client, "Second chat");

        CursorPage<ConversationDto>? firstPage = await client.GetFromJsonAsync<CursorPage<ConversationDto>>("/api/chats?limit=1");

        Assert.NotNull(firstPage);
        Assert.True(firstPage.HasMore);
        Assert.NotNull(firstPage.NextCursor);

        CursorPage<ConversationDto>? secondPage = await client.GetFromJsonAsync<CursorPage<ConversationDto>>(
            $"/api/chats?limit=1&cursor={Uri.EscapeDataString(firstPage.NextCursor)}");

        Assert.NotNull(secondPage);
        Assert.DoesNotContain(secondPage.Items, conversation => conversation.Id == firstPage.Items[0].Id);
        Guid[] returnedIds = firstPage.Items.Concat(secondPage.Items).Select(conversation => conversation.Id).ToArray();
        Assert.Contains(first.Id, returnedIds);
        Assert.Contains(second.Id, returnedIds);
    }

    [Fact]
    public async Task Notes_Should_Return_ValidationProblemDetails_For_Invalid_Cursor()
    {
        using LocalApiTestFactory factory = new();
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync("/api/notes?cursor=not-a-valid-cursor");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        ValidationProblemDetails? problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal("pagination.invalidCursor", problem.Extensions["code"]?.ToString());
        Assert.Contains("cursor", problem.Errors.Keys);
    }

    [Fact]
    public async Task Buckets_Page_Should_Reject_Cursor_When_Filter_Changes()
    {
        using LocalApiTestFactory factory = new();
        using HttpClient client = factory.CreateClient();
        await CreateBucketAsync(client, "Alpha A");
        await CreateBucketAsync(client, "Alpha B");
        await CreateBucketAsync(client, "Beta A");

        CursorPage<BucketDto>? firstPage = await client.GetFromJsonAsync<CursorPage<BucketDto>>(
            "/api/buckets/page?query=Alpha&limit=1");
        Assert.NotNull(firstPage);
        Assert.True(firstPage.HasMore);
        Assert.NotNull(firstPage.NextCursor);

        HttpResponseMessage response = await client.GetAsync(
            $"/api/buckets/page?query=Beta&limit=1&cursor={Uri.EscapeDataString(firstPage.NextCursor)}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        ValidationProblemDetails? problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal("pagination.invalidCursor", problem.Extensions["code"]?.ToString());
    }

    private static async Task<BucketDto> CreateBucketAsync(HttpClient client, string name)
    {
        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/buckets",
            new CreateBucketRequest($"{name}-{Guid.NewGuid():N}", Description: null));
        response.EnsureSuccessStatusCode();

        BucketDto? bucket = await response.Content.ReadFromJsonAsync<BucketDto>();
        Assert.NotNull(bucket);
        return bucket;
    }
}
