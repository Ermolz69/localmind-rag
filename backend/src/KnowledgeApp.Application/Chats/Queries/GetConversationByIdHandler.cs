using KnowledgeApp.Application.Abstractions;
using KnowledgeApp.Contracts.Chats;
using KnowledgeApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeApp.Application.Chats;

public sealed class GetConversationByIdHandler(IAppDbContext dbContext)
{
    public async Task<ConversationDto?> HandleAsync(Guid conversationId, CancellationToken cancellationToken = default)
    {
        Conversation? conversation = await dbContext.Conversations
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == conversationId, cancellationToken);

        return conversation is null ? null : ConversationMapper.ToDto(conversation);
    }
}
