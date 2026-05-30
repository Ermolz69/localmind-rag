using KnowledgeApp.Application.Abstractions;
using KnowledgeApp.Application.Common.Errors;
using KnowledgeApp.Application.Common.Results;
using KnowledgeApp.Contracts.Chats;
using KnowledgeApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeApp.Application.Chats;

public sealed class UpdateConversationHandler(
    IConversationRepository conversationRepository,
    IUnitOfWork unitOfWork,
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

        Conversation? conversation = await conversationRepository.GetByIdAsync(conversationId, cancellationToken);
        if (conversation is null)
        {
            return Result.Failure(ApplicationErrors.NotFound(ErrorCodes.Chats.NotFound, ErrorMessages.Chats.NotFound));
        }

        conversation.Title = request.Title.Trim();
        conversation.UpdatedAt = dateTimeProvider.UtcNow;
        await conversationRepository.UpdateAsync(conversation, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
