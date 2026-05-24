using KnowledgeApp.Application.Abstractions;
using KnowledgeApp.Application.Common.Errors;
using KnowledgeApp.Application.Common.Results;
using KnowledgeApp.Contracts.Chats;
using KnowledgeApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeApp.Application.Chats;

public sealed class UpdateConversationHandler(
    IAppDbContext dbContext,
    IDateTimeProvider dateTimeProvider,
    ChatRequestValidator validator)
{
    public async Task<Result> HandleAsync(
        Guid conversationId,
        UpdateConversationRequest request,
        CancellationToken cancellationToken = default)
    {
        Result validation = validator.Validate(request);
        if (!validation.IsSuccess)
        {
            return validation;
        }

        Conversation? conversation = await dbContext.Conversations
            .FirstOrDefaultAsync(item => item.Id == conversationId && item.DeletedAt == null, cancellationToken);
        if (conversation is null)
        {
            return Result.Failure(ApplicationErrors.NotFound(ErrorCodes.Chats.NotFound, ErrorMessages.Chats.NotFound));
        }

        conversation.Title = request.Title.Trim();
        conversation.UpdatedAt = dateTimeProvider.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
