using KnowledgeApp.Application.Abstractions;
using KnowledgeApp.Application.Chats;
using KnowledgeApp.Contracts.Rag;
using KnowledgeApp.Domain.Entities;
using KnowledgeApp.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeApp.UnitTests.Chats;

public sealed class SendChatStreamMessageHandlerTests
{
    [Fact]
    public async Task HandleStreamAsync_Should_Add_Messages_And_Yield_Chunks()
    {
        await using ApplicationTestDatabase database = await ApplicationTestDatabase.CreateAsync();

        Conversation conversation = new Conversation { Title = "Test Chat" };
        database.Context.Conversations.Add(conversation);
        await database.Context.SaveChangesAsync();

        RagSourceDto[] sources = [new RagSourceDto(Guid.NewGuid(), "doc.pdf", Guid.NewGuid(), 1, 0.9, "snippet")];

        IRagAnswerGenerator ragGenerator = new StubRagAnswerGenerator(
            [new RagAnswerChunkDto("Hello", sources), new RagAnswerChunkDto(" world")]);

        SendChatStreamMessageHandler handler = new(
            database.Context,
            ragGenerator,
            new ChatRequestValidator(),
            new FixedDateTimeProvider(),
            new FakeLocalDeviceResolver());

        List<RagAnswerChunkDto> chunks = [];
        await foreach (var chunk in handler.HandleStreamAsync(conversation.Id, new ChatMessageRequest("Hi")))
        {
            chunks.Add(chunk);
        }

        Assert.Equal(2, chunks.Count);
        Assert.Equal("Hello", chunks[0].Text);
        Assert.Equal(sources, chunks[0].Sources);
        Assert.Equal(" world", chunks[1].Text);

        // Verify database
        List<ChatMessage> messages = await database.Context.ChatMessages
            .Where(m => m.ConversationId == conversation.Id)
            .ToListAsync();

        messages = messages.OrderBy(m => m.CreatedAt).ToList();

        Assert.Equal(2, messages.Count);
        Assert.Equal(ChatRole.User, messages[0].Role);
        Assert.Equal("Hi", messages[0].Content);
        Assert.Equal(ChatRole.Assistant, messages[1].Role);
        Assert.Equal("Hello world", messages[1].Content);
    }

    private sealed class StubRagAnswerGenerator(IEnumerable<RagAnswerChunkDto> chunks) : IRagAnswerGenerator
    {
        public Task<RagAnswerDto> AnswerAsync(Guid conversationId, string question, CancellationToken cancellationToken = default) =>
            Task.FromResult(new RagAnswerDto("answer", []));

        public async IAsyncEnumerable<RagAnswerChunkDto> AnswerStreamAsync(Guid conversationId, string question, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            foreach (var chunk in chunks)
            {
                yield return chunk;
                await Task.Yield();
            }
        }
    }
}
