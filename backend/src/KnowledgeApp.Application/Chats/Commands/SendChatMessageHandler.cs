using KnowledgeApp.Application.Abstractions;
using KnowledgeApp.Application.Exceptions;
using KnowledgeApp.Contracts.Rag;
using KnowledgeApp.Domain.Entities;
using KnowledgeApp.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeApp.Application.Chats;

public sealed class SendChatMessageHandler(
    IAppDbContext dbContext,
    IRagAnswerGenerator ragAnswerGenerator,
    ChatRequestValidator validator)
{
    public async Task<SendChatMessageResult> HandleAsync(
        Guid conversationId,
        ChatMessageRequest request,
        CancellationToken cancellationToken = default)
    {
        validator.Validate(request);

        bool conversationExists = await dbContext.Conversations
            .AsNoTracking()
            .AnyAsync(conversation => conversation.Id == conversationId, cancellationToken);
        if (!conversationExists)
        {
            throw new NotFoundAppException("chats.notFound", "Conversation was not found.");
        }

        dbContext.ChatMessages.Add(new ChatMessage
        {
            ConversationId = conversationId,
            Role = ChatRole.User,
            Content = request.Content.Trim(),
        });

        RagAnswerDto answer = await ragAnswerGenerator.AnswerAsync(conversationId, request.Content.Trim(), cancellationToken);

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
