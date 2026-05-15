using KnowledgeApp.Application.Abstractions;
using KnowledgeApp.Application.Chats;
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
        await using var database = await ApplicationTestDatabase.CreateAsync();
        var create = new CreateChatHandler(database.Context);
        var list = new GetChatsHandler(database.Context);
        var send = new SendChatMessageHandler(database.Context, new FakeRagAnswerGenerator());

        var conversation = await create.HandleAsync(new CreateConversationRequest("Question"));
        var conversations = await list.HandleAsync();
        var answer = await send.HandleAsync(conversation.Id, new ChatMessageRequest("What is local RAG?"));

        var messages = await database.Context.ChatMessages.ToArrayAsync();

        Assert.Contains(conversations, item => item.Id == conversation.Id);
        Assert.Equal("Stub answer", answer.Answer.Answer);
        Assert.Contains(messages, message => message.Role == ChatRole.User && message.Content == "What is local RAG?");
        Assert.Contains(messages, message => message.Role == ChatRole.Assistant && message.Content == "Stub answer");
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
