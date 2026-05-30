using KnowledgeApp.Application.Abstractions;
using KnowledgeApp.Application.Chats;
using KnowledgeApp.Application.Common.Results;
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
        var conversationRepository = new KnowledgeApp.Infrastructure.Services.Persistence.ConversationRepository(database.Context);
        var unitOfWork = new KnowledgeApp.Infrastructure.Services.UnitOfWork(database.Context);
        CreateChatHandler create = new(conversationRepository, unitOfWork, validator, new FakeLocalDeviceResolver());
        GetChatsHandler list = new(conversationRepository);
        SendChatMessageHandler send = new(
            conversationRepository,
            unitOfWork,
            new FakeRagAnswerGenerator(),
            validator,
            new FixedDateTimeProvider(),
            new FakeLocalDeviceResolver());

        ConversationDto? conversation = (await create.HandleAsync(new CreateConversationRequest("Question"))).AssertSuccess();
        Contracts.Common.CursorPage<ConversationDto> conversations = (await list.HandleAsync(new GetChatsQuery())).AssertSuccess();
        RagAnswerDto? answer = (await send.HandleAsync(conversation.Id, new ChatMessageRequest("What is local RAG?"))).AssertSuccess();

        Domain.Entities.ChatMessage[]? messages = await database.Context.ChatMessages.ToArrayAsync();

        Assert.Contains(conversations.Items, item => item.Id == conversation.Id);
        Assert.Equal("Stub answer", answer.Answer);
        Assert.Single(answer.Sources);
        Assert.Equal("Matched chunk", answer.Sources[0].Snippet);
        Assert.Contains(messages, message => message.Role == ChatRole.User && message.Content == "What is local RAG?");
        Assert.Contains(messages, message => message.Role == ChatRole.Assistant && message.Content == "Stub answer");
    }

    [Fact]
    public async Task SendChatMessageHandler_Should_Reject_Missing_Conversation()
    {
        await using ApplicationTestDatabase database = await ApplicationTestDatabase.CreateAsync();
        var conversationRepository = new KnowledgeApp.Infrastructure.Services.Persistence.ConversationRepository(database.Context);
        var unitOfWork = new KnowledgeApp.Infrastructure.Services.UnitOfWork(database.Context);
        SendChatMessageHandler send = new(
            conversationRepository,
            unitOfWork,
            new FakeRagAnswerGenerator(),
            new ChatRequestValidator(),
            new FixedDateTimeProvider(),
            new FakeLocalDeviceResolver());

        Result<RagAnswerDto> result = await send.HandleAsync(Guid.NewGuid(), new ChatMessageRequest("Hello"));

        Assert.Equal("CHAT_NOT_FOUND", result.AssertFailure(ErrorType.NotFound).Code);
    }

    private sealed class FakeRagAnswerGenerator : IRagAnswerGenerator
    {
        public Task<RagAnswerDto> AnswerAsync(
            Guid conversationId,
            string question,
            CancellationToken cancellationToken = default)
        {
            RagSourceDto source = new(
                Guid.NewGuid(),
                "Document",
                Guid.NewGuid(),
                PageNumber: null,
                Score: 0.95,
                "Matched chunk");

            return Task.FromResult(new RagAnswerDto("Stub answer", [source]));
        }

        public async IAsyncEnumerable<RagAnswerChunkDto> AnswerStreamAsync(
            Guid conversationId,
            string question,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            yield return new RagAnswerChunkDto("Stub answer");
            await Task.Yield();
        }
    }
}
