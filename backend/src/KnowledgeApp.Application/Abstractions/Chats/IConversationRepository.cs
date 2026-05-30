using KnowledgeApp.Domain.Entities;

namespace KnowledgeApp.Application.Abstractions;

public interface IConversationRepository
{
    Task<Conversation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Conversation>> ListAsync(CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAsync(Conversation conversation, CancellationToken cancellationToken = default);
    Task UpdateAsync(Conversation conversation, CancellationToken cancellationToken = default);
    Task DeleteAsync(Conversation conversation, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ChatMessage>> GetMessagesAsync(Guid conversationId, CancellationToken cancellationToken = default);
    Task AddMessageAsync(ChatMessage message, CancellationToken cancellationToken = default);
    Task DeleteMessagesAsync(IEnumerable<ChatMessage> messages, CancellationToken cancellationToken = default);
}
