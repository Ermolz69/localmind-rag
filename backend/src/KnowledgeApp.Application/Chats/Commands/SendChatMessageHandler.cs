using KnowledgeApp.Application.Abstractions;
using KnowledgeApp.Contracts.Rag;
using KnowledgeApp.Domain.Entities;
using KnowledgeApp.Domain.Enums;

namespace KnowledgeApp.Application.Chats;

public sealed class SendChatMessageHandler(IAppDbContext dbContext, IRagAnswerGenerator ragAnswerGenerator)
{
    public async Task<SendChatMessageResult> HandleAsync(
        Guid conversationId,
        ChatMessageRequest request,
        CancellationToken cancellationToken = default)
    {
        dbContext.ChatMessages.Add(new ChatMessage
        {
            ConversationId = conversationId,
            Role = ChatRole.User,
            Content = request.Content,
        });

        var answer = await ragAnswerGenerator.AnswerAsync(conversationId, request.Content, cancellationToken);

        dbContext.ChatMessages.Add(new ChatMessage
        {
            ConversationId = conversationId,
            Role = ChatRole.Assistant,
            Content = answer.Answer,
        });

        await dbContext.SaveChangesAsync(cancellationToken);
        return new SendChatMessageResult(answer);
    }
}
