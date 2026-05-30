using KnowledgeApp.Application.Abstractions;
using KnowledgeApp.Application.Common.Errors;
using KnowledgeApp.Application.Common.Results;
using KnowledgeApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeApp.Application.Chats;

public sealed class DeleteConversationHandler(
    IConversationRepository conversationRepository,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider)
{
    public async Task<Result> HandleAsync(
        Guid conversationId,
        CancellationToken cancellationToken = default)
    {
        Conversation? conversation = await conversationRepository.GetByIdAsync(conversationId, cancellationToken);
        if (conversation is null)
        {
            return Result.Failure(ApplicationErrors.NotFound(ErrorCodes.Chats.NotFound, ErrorMessages.Chats.NotFound));
        }

        DateTimeOffset now = dateTimeProvider.UtcNow;
        conversation.DeletedAt = now;
        conversation.UpdatedAt = now;

        IReadOnlyList<ChatMessage> messages = await conversationRepository.GetMessagesAsync(conversationId, cancellationToken);
        foreach (ChatMessage message in messages)
        {
            message.DeletedAt = now;
            message.UpdatedAt = now;
        }

        await conversationRepository.UpdateAsync(conversation, cancellationToken);
        // ChatMessage updates will be tracked and saved by EF Core when SaveChangesAsync is called on unit of work.
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
