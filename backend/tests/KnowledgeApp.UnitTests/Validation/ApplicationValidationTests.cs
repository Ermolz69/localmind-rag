using KnowledgeApp.Application.Buckets;
using KnowledgeApp.Application.Common.Errors;
using KnowledgeApp.Application.Chats;
using KnowledgeApp.Application.Exceptions;
using KnowledgeApp.Application.Notes;
using KnowledgeApp.Application.Search;
using KnowledgeApp.Contracts.Buckets;
using KnowledgeApp.Contracts.Chats;
using KnowledgeApp.Contracts.Notes;
using KnowledgeApp.Contracts.Rag;

namespace KnowledgeApp.UnitTests.Validation;

public sealed class ApplicationValidationTests
{
    [Fact]
    public void BucketValidator_Should_Reject_Blank_Name()
    {
        BucketRequestValidator validator = new();

        ValidationAppException exception = Assert.Throws<ValidationAppException>(
            () => validator.Validate(new CreateBucketRequest(" ", null)));

        Assert.Equal(ErrorCodes.Buckets.ValidationFailed, exception.Code);
        Assert.Contains("name", exception.Errors.Keys);
        Assert.Equal(ErrorMessages.Buckets.NameRequired, exception.Errors["name"].Single());
    }

    [Fact]
    public void NoteValidator_Should_Reject_Too_Large_Markdown()
    {
        NoteRequestValidator validator = new();
        string markdown = new('a', 1_000_001);

        ValidationAppException exception = Assert.Throws<ValidationAppException>(
            () => validator.Validate(new CreateNoteRequest(null, "Title", markdown)));

        Assert.Equal(ErrorCodes.Notes.ValidationFailed, exception.Code);
        Assert.Contains("markdown", exception.Errors.Keys);
        Assert.Equal(ErrorMessages.Notes.MarkdownTooLong, exception.Errors["markdown"].Single());
    }

    [Fact]
    public void ChatValidator_Should_Reject_Empty_Message()
    {
        ChatRequestValidator validator = new();

        ValidationAppException exception = Assert.Throws<ValidationAppException>(
            () => validator.Validate(new ChatMessageRequest("")));

        Assert.Equal(ErrorCodes.Chats.ValidationFailed, exception.Code);
        Assert.Contains("content", exception.Errors.Keys);
        Assert.Equal(ErrorMessages.Chats.ContentRequired, exception.Errors["content"].Single());
    }

    [Fact]
    public void SearchValidator_Should_Reject_Invalid_Limit()
    {
        SemanticSearchRequestValidator validator = new();

        ValidationAppException exception = Assert.Throws<ValidationAppException>(
            () => validator.Validate(new SemanticSearchRequest("query", Limit: 51)));

        Assert.Equal(ErrorCodes.Search.ValidationFailed, exception.Code);
        Assert.Contains("limit", exception.Errors.Keys);
        Assert.Equal(ErrorMessages.Search.LimitOutOfRange, exception.Errors["limit"].Single());
    }

    [Fact]
    public void SearchValidator_Should_Reject_Blank_Query_With_Stable_Field_Error()
    {
        SemanticSearchRequestValidator validator = new();

        ValidationAppException exception = Assert.Throws<ValidationAppException>(
            () => validator.Validate(new SemanticSearchRequest(" ")));

        Assert.Equal(ErrorCodes.Search.ValidationFailed, exception.Code);
        Assert.Equal(ErrorMessages.Search.RequestInvalid, exception.Message);
        Assert.Equal(ErrorMessages.Search.QueryRequired, exception.Errors[SemanticSearchRequestValidator.QueryField].Single());
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
