using KnowledgeApp.Application.Buckets;
using KnowledgeApp.Application.Chats;
using KnowledgeApp.Application.Common.Errors;
using KnowledgeApp.Application.Common.Results;
using KnowledgeApp.Application.Exceptions;
using KnowledgeApp.Application.Notes;
using KnowledgeApp.Application.Search;
using KnowledgeApp.Contracts.Buckets;
using KnowledgeApp.Contracts.Chats;
using KnowledgeApp.Contracts.Notes;
using KnowledgeApp.Contracts.Rag;
using KnowledgeApp.Contracts.Search;

namespace KnowledgeApp.UnitTests.Validation;

public sealed class ApplicationValidationTests
{
    [Fact]
    public void BucketValidator_Should_Reject_Blank_Name()
    {
        BucketRequestValidator validator = new();

        Result result = validator.Validate(new CreateBucketRequest(" ", null));
        ApplicationError error = result.AssertFailure(ErrorType.Validation);

        Assert.Equal(ErrorCodes.Buckets.ValidationFailed, error.Code);
        Assert.Contains(error.Details!, detail => detail.Field == "name" && detail.Message == ErrorMessages.Buckets.NameRequired);
    }

    [Fact]
    public void NoteValidator_Should_Reject_Too_Large_Markdown()
    {
        NoteRequestValidator validator = new();
        string markdown = new('a', 1_000_001);

        Result result = validator.Validate(new CreateNoteRequest(null, "Title", markdown));
        ApplicationError error = result.AssertFailure(ErrorType.Validation);

        Assert.Equal(ErrorCodes.Notes.ValidationFailed, error.Code);
        Assert.Contains(error.Details!, detail => detail.Field == "markdown" && detail.Message == ErrorMessages.Notes.MarkdownTooLong);
    }

    [Fact]
    public void ChatValidator_Should_Reject_Empty_Message()
    {
        ChatRequestValidator validator = new();

        Result result = validator.Validate(new ChatMessageRequest(""));
        ApplicationError error = result.AssertFailure(ErrorType.Validation);

        Assert.Equal(ErrorCodes.Chats.ValidationFailed, error.Code);
        Assert.Contains(error.Details!, detail => detail.Field == "content" && detail.Message == ErrorMessages.Chats.ContentRequired);
    }

    [Fact]
    public void ChatValidator_Should_Reject_Invalid_Filter_File_Type()
    {
        ChatRequestValidator validator = new();

        Result result = validator.Validate(new ChatMessageRequest("Question", new RetrievalFilters(FileType: "exe")));
        ApplicationError error = result.AssertFailure(ErrorType.Validation);

        Assert.Equal(ErrorCodes.Chats.ValidationFailed, error.Code);
        Assert.Contains(error.Details!, detail => detail.Field == "filters.fileType");
    }

    [Fact]
    public void ChatValidator_Should_Reject_Invalid_Filter_Date_Range()
    {
        ChatRequestValidator validator = new();

        Result result = validator.Validate(new ChatMessageRequest(
            "Question",
            new RetrievalFilters(
                DateFrom: new DateTimeOffset(2026, 06, 10, 0, 0, 0, TimeSpan.Zero),
                DateTo: new DateTimeOffset(2026, 06, 09, 0, 0, 0, TimeSpan.Zero))));
        ApplicationError error = result.AssertFailure(ErrorType.Validation);

        Assert.Equal(ErrorCodes.Chats.ValidationFailed, error.Code);
        Assert.Contains(error.Details!, detail => detail.Field == "filters.dateFrom");
    }

    [Fact]
    public void SearchValidator_Should_Reject_Invalid_Limit()
    {
        SemanticSearchRequestValidator validator = new();

        Result result = validator.Validate(new SemanticSearchRequest("query", Limit: 51));
        ApplicationError error = result.AssertFailure(ErrorType.Validation);

        Assert.Equal(ErrorCodes.Search.ValidationFailed, error.Code);
        Assert.Contains(error.Details!, detail => detail.Field == "limit" && detail.Message == ErrorMessages.Search.LimitOutOfRange);
    }

    [Fact]
    public void SearchValidator_Should_Reject_Blank_Query_With_Stable_Field_Error()
    {
        SemanticSearchRequestValidator validator = new();

        Result result = validator.Validate(new SemanticSearchRequest(" "));
        ApplicationError error = result.AssertFailure(ErrorType.Validation);

        Assert.Equal(ErrorCodes.Search.ValidationFailed, error.Code);
        Assert.Equal(ErrorMessages.Search.RequestInvalid, error.Message);
        Assert.Contains(error.Details!, detail =>
            detail.Field == SemanticSearchRequestValidator.QueryField &&
            detail.Message == ErrorMessages.Search.QueryRequired);
    }

    [Fact]
    public void AppExceptions_Should_Preserve_Code_Message_And_Errors()
    {
        Dictionary<string, string[]> errors = new() { ["query"] = [ErrorMessages.Search.QueryRequired] };

        ValidationAppException validation = new(ErrorCodes.Search.ValidationFailed, ErrorMessages.Search.RequestInvalid, errors);
        NotFoundAppException notFound = new(ErrorCodes.Buckets.NotFound, ErrorMessages.Buckets.NotFound);
        ExternalDependencyAppException external = new(
            ErrorCodes.Runtime.ExternalDependencyUnavailable,
            ErrorMessages.Runtime.ExternalDependencyUnavailable);

        Assert.Equal(ErrorCodes.Search.ValidationFailed, validation.Code);
        Assert.Equal(ErrorMessages.Search.RequestInvalid, validation.Message);
        Assert.Equal(ErrorMessages.Search.QueryRequired, validation.Errors["query"].Single());
        Assert.Equal(ErrorCodes.Buckets.NotFound, notFound.Code);
        Assert.Equal(ErrorMessages.Buckets.NotFound, notFound.Message);
        Assert.Equal(ErrorCodes.Runtime.ExternalDependencyUnavailable, external.Code);
    }
}
