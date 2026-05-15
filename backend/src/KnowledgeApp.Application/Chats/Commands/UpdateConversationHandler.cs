using KnowledgeApp.Application.Abstractions;
using KnowledgeApp.Contracts.Chats;
using KnowledgeApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeApp.Application.Chats;

public sealed class UpdateConversationHandler(
    IAppDbContext dbContext,
    IDateTimeProvider dateTimeProvider,
    ChatRequestValidator validator)
{
    public async Task<UpdateConversationResult> HandleAsync(
        Guid conversationId,
        UpdateConversationRequest request,
        CancellationToken cancellationToken = default)
    {
        validator.Validate(request);

        Conversation? conversation = await dbContext.Conversations
            .FirstOrDefaultAsync(item => item.Id == conversationId, cancellationToken);
        if (conversation is null)
        {
            return new UpdateConversationResult(false);
        }

        conversation.Title = request.Title.Trim();
        conversation.UpdatedAt = dateTimeProvider.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        return new UpdateConversationResult(true);
    }
}
