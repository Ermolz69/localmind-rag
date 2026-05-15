using KnowledgeApp.Application.Abstractions;
using KnowledgeApp.Contracts.Chats;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeApp.Application.Chats;

public sealed class GetChatsHandler(IAppDbContext dbContext)
{
    public async Task<IReadOnlyList<ConversationDto>> HandleAsync(CancellationToken cancellationToken = default)
    {
        var conversations = await dbContext.Conversations
            .AsNoTracking()
            .ToArrayAsync(cancellationToken);

        return conversations.Select(ConversationMapper.ToDto).ToArray();
    }
}
