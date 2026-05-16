using KnowledgeApp.Application.Abstractions;
using KnowledgeApp.Application.Chats;
using KnowledgeApp.Application.Exceptions;
using KnowledgeApp.Contracts.Chats;
using KnowledgeApp.Contracts.Rag;
using KnowledgeApp.Domain.Enums;
using KnowledgeApp.UnitTests;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeApp.UnitTests.Chats;

public sealed class ChatHandlersTests
{
    [Fact]
    public async Task ChatHandlers_Should_Create_List_And_Save_User_And_Assistant_Messages()
    {
        await using ApplicationTestDatabase? database = await ApplicationTestDatabase.CreateAsync();
        ChatRequestValidator validator = new();
        CreateChatHandler create = new(database.Context, validator, new FakeLocalDeviceResolver());
        GetChatsHandler list = new(database.Context);
        SendChatMessageHandler send = new(
            database.Context,
            new FakeRagAnswerGenerator(),
            validator,
            new FixedDateTimeProvider(),
            new FakeLocalDeviceResolver());

        ConversationDto? conversation = await create.HandleAsync(new CreateConversationRequest("Question"));
        Contracts.Common.CursorPage<ConversationDto> conversations = await list.HandleAsync(new GetChatsQuery());
        SendChatMessageResult? answer = await send.HandleAsync(conversation.Id, new ChatMessageRequest("What is local RAG?"));

        Domain.Entities.ChatMessage[]? messages = await database.Context.ChatMessages.ToArrayAsync();

        Assert.Contains(conversations.Items, item => item.Id == conversation.Id);
        Assert.Equal("Stub answer", answer.Answer.Answer);
        Assert.Contains(messages, message => message.Role == ChatRole.User && message.Content == "What is local RAG?");
        Assert.Contains(messages, message => message.Role == ChatRole.Assistant && message.Content == "Stub answer");
    }

    [Fact]
    public async Task SendChatMessageHandler_Should_Reject_Missing_Conversation()
    {
        await using ApplicationTestDatabase database = await ApplicationTestDatabase.CreateAsync();
        SendChatMessageHandler send = new(
            database.Context,
            new FakeRagAnswerGenerator(),
            new ChatRequestValidator(),
            new FixedDateTimeProvider(),
            new FakeLocalDeviceResolver());

        NotFoundAppException exception = await Assert.ThrowsAsync<NotFoundAppException>(
            () => send.HandleAsync(Guid.NewGuid(), new ChatMessageRequest("Hello")));

        Assert.Equal("chats.notFound", exception.Code);
    }

    private sealed class FakeRagAnswerGenerator : IRagAnswerGenerator
    {
        public Task<RagAnswerDto> AnswerAsync(
            Guid conversationId,
            string question,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new RagAnswerDto("Stub answer", []));
        }
    }
}
