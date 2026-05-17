using KnowledgeApp.Application.Abstractions;
using KnowledgeApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeApp.Application.Chats;

public sealed class DeleteConversationHandler(IAppDbContext dbContext, IDateTimeProvider dateTimeProvider)
{
    public async Task<DeleteConversationResult> HandleAsync(
        Guid conversationId,
        CancellationToken cancellationToken = default)
    {
        Conversation? conversation = await dbContext.Conversations
            .FirstOrDefaultAsync(item => item.Id == conversationId && item.DeletedAt == null, cancellationToken);
        if (conversation is null)
        {
            return new DeleteConversationResult(false);
        }

        DateTimeOffset now = dateTimeProvider.UtcNow;
        conversation.DeletedAt = now;
        conversation.UpdatedAt = now;

        ChatMessage[] messages = await dbContext.ChatMessages
            .Where(message => message.ConversationId == conversationId && message.DeletedAt == null)
            .ToArrayAsync(cancellationToken);
        foreach (ChatMessage message in messages)
        {
            message.DeletedAt = now;
            message.UpdatedAt = now;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return new DeleteConversationResult(true);
    }
}
