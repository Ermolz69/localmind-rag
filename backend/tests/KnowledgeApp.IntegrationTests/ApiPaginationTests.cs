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

        CursorPage<ConversationDto>? firstPage = await client.GetApiDataAsync<CursorPage<ConversationDto>>(
            "/api/v1/chats?limit=1");

        Assert.NotNull(firstPage);
        Assert.True(firstPage.HasMore);
        Assert.NotNull(firstPage.NextCursor);

        CursorPage<ConversationDto>? secondPage = await client.GetApiDataAsync<CursorPage<ConversationDto>>(
            $"/api/v1/chats?limit=1&cursor={Uri.EscapeDataString(firstPage.NextCursor)}");

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

        HttpResponseMessage response = await client.GetAsync("/api/v1/notes?cursor=not-a-valid-cursor");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        ApiResponse<object?> envelope = await response.Content.ReadApiErrorAsync();
        Assert.Equal("VALIDATION_FAILED", envelope.Error!.Code);
        Assert.Contains(envelope.Error.Details ?? [], detail => detail.Field == "cursor");
    }

    [Fact]
    public async Task Buckets_Page_Should_Reject_Cursor_When_Filter_Changes()
    {
        using LocalApiTestFactory factory = new();
        using HttpClient client = factory.CreateClient();
        await CreateBucketAsync(client, "Alpha A");
        await CreateBucketAsync(client, "Alpha B");
        await CreateBucketAsync(client, "Beta A");

        CursorPage<BucketDto>? firstPage = await client.GetApiDataAsync<CursorPage<BucketDto>>(
            "/api/v1/buckets/page?query=Alpha&limit=1");

        Assert.NotNull(firstPage);
        Assert.True(firstPage.HasMore);
        Assert.NotNull(firstPage.NextCursor);

        HttpResponseMessage response = await client.GetAsync(
            $"/api/v1/buckets/page?query=Beta&limit=1&cursor={Uri.EscapeDataString(firstPage.NextCursor)}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        ApiResponse<object?> envelope = await response.Content.ReadApiErrorAsync();
        Assert.Equal("VALIDATION_FAILED", envelope.Error!.Code);
    }

    private static async Task<BucketDto> CreateBucketAsync(HttpClient client, string name)
    {
        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/v1/buckets",
            new CreateBucketRequest($"{name}-{Guid.NewGuid():N}", Description: null));

        response.EnsureSuccessStatusCode();

        BucketDto? bucket = await response.Content.ReadApiDataAsync<BucketDto>();

        Assert.NotNull(bucket);

        return bucket;
    }
}
