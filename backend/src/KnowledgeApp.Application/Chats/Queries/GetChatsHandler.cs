using KnowledgeApp.Application.Abstractions;
using KnowledgeApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeApp.Application.Chats;

public sealed class GetChatsHandler(IAppDbContext dbContext)
{
    public async Task<IReadOnlyList<Conversation>> HandleAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.Conversations.AsNoTracking().ToArrayAsync(cancellationToken);
    }
}
