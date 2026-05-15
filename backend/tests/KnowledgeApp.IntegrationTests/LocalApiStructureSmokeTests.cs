using System.Net;
using System.Net.Http.Json;
using KnowledgeApp.Domain.Entities;
using KnowledgeApp.Domain.Enums;
using KnowledgeApp.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace KnowledgeApp.IntegrationTests;

public sealed class LocalApiStructureSmokeTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> factory;

    public LocalApiStructureSmokeTests(WebApplicationFactory<Program> factory)
    {
        this.factory = factory;
    }

    [Fact]
    public async Task Bucket_Note_And_Chat_Endpoints_Should_Keep_Current_Wire_Shape()
    {
        using var client = factory.CreateClient();

        var bucketResponse = await client.PostAsJsonAsync("/api/buckets", new Bucket { Name = $"Bucket-{Guid.NewGuid():N}" });
        var noteResponse = await client.PostAsJsonAsync("/api/notes", new Note { Title = "Note", Markdown = "Body" });
        var chatResponse = await client.PostAsJsonAsync("/api/chats", new Conversation { Title = "Chat" });

        Assert.Equal(HttpStatusCode.Created, bucketResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Created, noteResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Created, chatResponse.StatusCode);
        Assert.NotNull(await client.GetFromJsonAsync<Bucket[]>("/api/buckets"));
        Assert.NotNull(await client.GetFromJsonAsync<Note[]>("/api/notes"));
        Assert.NotNull(await client.GetFromJsonAsync<Conversation[]>("/api/chats"));
    }

    [Fact]
    public async Task ReindexDocumentEndpoint_Should_Create_Queued_Ingestion_Job()
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var document = new Document { Name = $"reindex-{Guid.NewGuid():N}.txt", Status = DocumentStatus.Indexed };
        db.Documents.Add(document);
        await db.SaveChangesAsync();

        using var client = factory.CreateClient();

        var response = await client.PostAsync($"/api/documents/{document.Id}/reindex", content: null);

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        Assert.True(await db.IngestionJobs.AnyAsync(job => job.DocumentId == document.Id));
    }
}
