using KnowledgeApp.Application.Abstractions;
using KnowledgeApp.Domain.Entities;

namespace KnowledgeApp.Application.Chats;

public sealed class CreateChatHandler(IAppDbContext dbContext)
{
    public async Task<Conversation> HandleAsync(Conversation conversation, CancellationToken cancellationToken = default)
    {
        dbContext.Conversations.Add(conversation);
        await dbContext.SaveChangesAsync(cancellationToken);
        return conversation;
    }
}
