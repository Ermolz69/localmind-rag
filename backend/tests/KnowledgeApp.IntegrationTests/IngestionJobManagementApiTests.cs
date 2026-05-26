using System.Net;
using System.Net.Http.Json;

using KnowledgeApp.Contracts.Common;
using KnowledgeApp.Contracts.Documents;
using KnowledgeApp.Contracts.Ingestion;

namespace KnowledgeApp.IntegrationTests;

public sealed class IngestionJobManagementApiTests(LocalApiTestFactory factory)
    : IClassFixture<LocalApiTestFactory>
{
    [Fact]
    public async Task IngestionJob_Endpoints_Should_List_Get_Cancel_And_Retry()
    {
        using HttpClient client = factory.CreateClient();

        UploadDocumentResponse upload = await UploadTextAsync(client);

        IngestionJobListResponse? list =
            await client.GetApiDataAsync<IngestionJobListResponse>("/api/v1/ingestion/jobs");

        Assert.Contains(list?.Items ?? [], item => item.Id == upload.IngestionJobId);

        IngestionJobDto? job = await client.GetApiDataAsync<IngestionJobDto>(
            $"/api/v1/ingestion/jobs/{upload.IngestionJobId}");

        Assert.NotNull(job);
        Assert.True(job.CanCancel);
        Assert.Equal("Pending", job.Status);
        Assert.Equal(0, job.ProgressPercent);
        Assert.Equal("Pending", job.CurrentStep);
        Assert.Null(job.ErrorCode);
        Assert.Null(job.ErrorMessage);

        using HttpResponseMessage cancelResponse = await client.PostAsync(
            $"/api/v1/ingestion/jobs/{upload.IngestionJobId}/cancel",
            null);

        Assert.Equal(HttpStatusCode.OK, cancelResponse.StatusCode);

        IngestionJobActionResponse? cancel =
            await cancelResponse.Content.ReadApiDataAsync<IngestionJobActionResponse>();

        Assert.Equal("Cancelled", cancel?.Status);

        using HttpResponseMessage retryResponse = await client.PostAsync(
            $"/api/v1/ingestion/jobs/{upload.IngestionJobId}/retry",
            null);

        Assert.Equal(HttpStatusCode.Accepted, retryResponse.StatusCode);

        IngestionJobActionResponse? retry =
            await retryResponse.Content.ReadApiDataAsync<IngestionJobActionResponse>();

        Assert.Equal("Pending", retry?.Status);
    }

    [Fact]
    public async Task IngestionJob_Get_Should_Return_NotFound_Envelope()
    {
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage response =
            await client.GetAsync($"/api/v1/ingestion/jobs/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        ApiResponse<object?> envelope =
            await response.Content.ReadApiErrorAsync();

        Assert.Equal("INGESTION_JOB_NOT_FOUND", envelope.Error?.Code);
    }

    private static async Task<UploadDocumentResponse> UploadTextAsync(HttpClient client)
    {
        using MultipartFormDataContent form = new();
        using ByteArrayContent file = new("ingestion management"u8.ToArray());

        file.Headers.ContentType =
            new System.Net.Http.Headers.MediaTypeHeaderValue("text/plain");

        form.Add(file, "file", $"managed-{Guid.NewGuid():N}.txt");

        using HttpResponseMessage response =
            await client.PostAsync("/api/v1/documents/upload", form);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        UploadDocumentResponse? upload =
            await response.Content.ReadApiDataAsync<UploadDocumentResponse>();

        Assert.NotNull(upload);

        return upload;
    }
}
