using KnowledgeApp.Application.Abstractions;
using KnowledgeApp.Contracts.Chats;
using KnowledgeApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeApp.Application.Chats;

public sealed class GetChatMessagesHandler(IAppDbContext dbContext)
{
    public async Task<IReadOnlyList<ChatMessageDto>?> HandleAsync(
        Guid conversationId,
        CancellationToken cancellationToken = default)
    {
        bool conversationExists = await dbContext.Conversations
            .AsNoTracking()
            .AnyAsync(conversation => conversation.Id == conversationId, cancellationToken);
        if (!conversationExists)
        {
            return null;
        }

        ChatMessage[] messages = await dbContext.ChatMessages
            .AsNoTracking()
            .Where(message => message.ConversationId == conversationId)
            .ToArrayAsync(cancellationToken);

        return messages
            .OrderBy(message => message.CreatedAt)
            .ThenBy(message => message.Id.ToString("N", System.Globalization.CultureInfo.InvariantCulture))
            .Select(ConversationMapper.ToDto)
            .ToArray();
    }
}
