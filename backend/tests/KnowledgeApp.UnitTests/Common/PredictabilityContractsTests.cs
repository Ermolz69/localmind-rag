using System.Text.Json;
using KnowledgeApp.Application.Common.Results;
using KnowledgeApp.Contracts.Common;
using KnowledgeApp.LocalApi.Endpoints;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace KnowledgeApp.UnitTests.Common;

public sealed class PredictabilityContractsTests
{
    [Fact]
    public void Result_Should_Model_Success_And_Failure()
    {
        Result<string> success = Result<string>.Success("ok");
        ApplicationError error = ApplicationErrors.NotFound("DOCUMENT_NOT_FOUND", "Document was not found.");
        Result<string> failure = Result<string>.Failure(error);

        Assert.True(success.IsSuccess);
        Assert.Equal("ok", success.Value);
        Assert.Null(success.Error);
        Assert.False(failure.IsSuccess);
        Assert.Null(failure.Value);
        Assert.Same(error, failure.Error);
    }

    [Fact]
    public void ApplicationErrors_Should_Create_Stable_Error_Metadata()
    {
        ApplicationError validation = ApplicationErrors.Validation(
            "VALIDATION_FAILED",
            "Request validation failed.",
            new Dictionary<string, string[]> { ["query"] = ["Query is required."] });
        ApplicationError conflict = ApplicationErrors.Conflict("CONFLICT", "Conflict.");

        Assert.Equal(ErrorType.Validation, validation.Type);
        Assert.Equal("VALIDATION_FAILED", validation.Code);
        Assert.Contains(validation.Details!, detail => detail.Field == "query" && detail.Message == "Query is required.");
        Assert.Equal(ErrorType.Conflict, conflict.Type);
    }

    [Fact]
    public void ApiResponse_Should_Create_Success_And_Failure_Envelopes()
    {
        ApiResponse<string> success = ApiResponse.Success("data", "request-id");
        ApiResponse<object?> failure = ApiResponse.Failure(
            "DOCUMENT_NOT_FOUND",
            "Document was not found.",
            "request-id",
            [new ApiErrorDetail("documentId", "Document does not exist.")]);

        Assert.True(success.Success);
        Assert.Equal("data", success.Data);
        Assert.Null(success.Error);
        Assert.Equal("request-id", success.Metadata.RequestId);
        Assert.False(failure.Success);
        Assert.Null(failure.Data);
        Assert.Equal("DOCUMENT_NOT_FOUND", failure.Error?.Code);
        Assert.Equal("documentId", failure.Error?.Details?.Single().Field);
    }

    [Theory]
    [InlineData(ErrorType.Validation, StatusCodes.Status400BadRequest)]
    [InlineData(ErrorType.NotFound, StatusCodes.Status404NotFound)]
    [InlineData(ErrorType.Conflict, StatusCodes.Status409Conflict)]
    [InlineData(ErrorType.UnsupportedMedia, StatusCodes.Status415UnsupportedMediaType)]
    [InlineData(ErrorType.Unprocessable, StatusCodes.Status422UnprocessableEntity)]
    [InlineData(ErrorType.NotImplemented, StatusCodes.Status501NotImplemented)]
    [InlineData(ErrorType.ExternalDependency, StatusCodes.Status503ServiceUnavailable)]
    [InlineData(ErrorType.Unexpected, StatusCodes.Status500InternalServerError)]
    public async Task ApiResults_Should_Map_Error_Types_To_Http_Status(ErrorType type, int expectedStatus)
    {
        DefaultHttpContext context = new();
        context.Response.Body = new MemoryStream();
        context.RequestServices = new ServiceCollection().AddLogging().BuildServiceProvider();
        ApplicationError error = new(type, $"{type.ToString().ToUpperInvariant()}_CODE", "Failure.");

        await ApiResults.Failure(error, context).ExecuteAsync(context);

        Assert.Equal(expectedStatus, context.Response.StatusCode);
        context.Response.Body.Position = 0;
        ApiResponse<object?>? envelope = await JsonSerializer.DeserializeAsync<ApiResponse<object?>>(
            context.Response.Body,
            new JsonSerializerOptions(JsonSerializerDefaults.Web));
        Assert.False(envelope?.Success);
        Assert.Equal(error.Code, envelope?.Error?.Code);
    }
}
