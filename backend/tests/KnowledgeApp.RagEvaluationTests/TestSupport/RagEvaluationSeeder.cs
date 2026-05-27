using System.Net;
using System.Text;

using KnowledgeApp.Application.Abstractions;
using KnowledgeApp.Contracts.Documents;

using Microsoft.Extensions.DependencyInjection;

namespace KnowledgeApp.RagEvaluationTests.TestSupport;

internal sealed class RagEvaluationSeeder(
    RagEvaluationTestFactory factory)
{
    public async Task SeedAsync(HttpClient client)
    {
        await factory.SeedGate.WaitAsync();

        try
        {
            if (factory.FixturesSeeded)
            {
                return;
            }

            foreach ((string fileName, string content) in RagFixtureLoader.LoadDocuments())
            {
                UploadDocumentResponse upload =
                    await UploadDocumentAsync(client, fileName, content);

                await using AsyncServiceScope scope =
                    factory.Services.CreateAsyncScope();

                IIngestionJobProcessor processor =
                    scope.ServiceProvider.GetRequiredService<IIngestionJobProcessor>();

                await processor.ProcessAsync(upload.IngestionJobId);
            }

            factory.FixturesSeeded = true;
        }
        finally
        {
            factory.SeedGate.Release();
        }
    }

    private static async Task<UploadDocumentResponse> UploadDocumentAsync(
        HttpClient client,
        string fileName,
        string content)
    {
        using MultipartFormDataContent form = new();

        using ByteArrayContent file =
            new(Encoding.UTF8.GetBytes(content));

        file.Headers.ContentType =
            new System.Net.Http.Headers.MediaTypeHeaderValue("text/plain");

        form.Add(file, "file", fileName);

        using HttpResponseMessage response =
            await client.PostAsync("/api/v1/documents/upload", form);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        UploadDocumentResponse? upload =
            await response.Content.ReadApiDataAsync<UploadDocumentResponse>();

        Assert.NotNull(upload);

        return upload;
    }
}
