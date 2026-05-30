using KnowledgeApp.Application.Abstractions;
using KnowledgeApp.Domain.Entities;
using KnowledgeApp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeApp.Infrastructure.Services.Persistence;

public sealed class ConversationRepository(AppDbContext dbContext) : IConversationRepository
{
    public async Task<Conversation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await dbContext.Conversations
            .FirstOrDefaultAsync(item => item.Id == id && item.DeletedAt == null, cancellationToken);
    }

    public async Task<IReadOnlyList<Conversation>> ListAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.Conversations
            .Where(conversation => conversation.DeletedAt == null)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await dbContext.Conversations
            .AnyAsync(conversation => conversation.Id == id && conversation.DeletedAt == null, cancellationToken);
    }

    public async Task AddAsync(Conversation conversation, CancellationToken cancellationToken = default)
    {
        await dbContext.Conversations.AddAsync(conversation, cancellationToken);
    }

    public Task UpdateAsync(Conversation conversation, CancellationToken cancellationToken = default)
    {
        dbContext.Conversations.Update(conversation);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Conversation conversation, CancellationToken cancellationToken = default)
    {
        dbContext.Conversations.Remove(conversation);
        return Task.CompletedTask;
    }

    public async Task<IReadOnlyList<ChatMessage>> GetMessagesAsync(Guid conversationId, CancellationToken cancellationToken = default)
    {
        return await dbContext.ChatMessages
            .Where(message => message.ConversationId == conversationId && message.DeletedAt == null)
            .ToListAsync(cancellationToken);
    }

    public async Task AddMessageAsync(ChatMessage message, CancellationToken cancellationToken = default)
    {
        await dbContext.ChatMessages.AddAsync(message, cancellationToken);
    }

    public Task DeleteMessagesAsync(IEnumerable<ChatMessage> messages, CancellationToken cancellationToken = default)
    {
        dbContext.ChatMessages.RemoveRange(messages);
        return Task.CompletedTask;
    }
}
