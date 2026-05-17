using KnowledgeApp.Application.Buckets;
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

        Assert.Equal("buckets.validationFailed", exception.Code);
        Assert.Contains("name", exception.Errors.Keys);
    }

    [Fact]
    public void NoteValidator_Should_Reject_Too_Large_Markdown()
    {
        NoteRequestValidator validator = new();
        string markdown = new('a', 1_000_001);

        ValidationAppException exception = Assert.Throws<ValidationAppException>(
            () => validator.Validate(new CreateNoteRequest(null, "Title", markdown)));

        Assert.Equal("notes.validationFailed", exception.Code);
        Assert.Contains("markdown", exception.Errors.Keys);
    }

    [Fact]
    public void ChatValidator_Should_Reject_Empty_Message()
    {
        ChatRequestValidator validator = new();

        ValidationAppException exception = Assert.Throws<ValidationAppException>(
            () => validator.Validate(new ChatMessageRequest("")));

        Assert.Equal("chats.validationFailed", exception.Code);
        Assert.Contains("content", exception.Errors.Keys);
    }

    [Fact]
    public void SearchValidator_Should_Reject_Invalid_Limit()
    {
        SemanticSearchRequestValidator validator = new();

        ValidationAppException exception = Assert.Throws<ValidationAppException>(
            () => validator.Validate(new SemanticSearchRequest("query", Limit: 51)));

        Assert.Equal("search.validationFailed", exception.Code);
        Assert.Contains("limit", exception.Errors.Keys);
    }
}
