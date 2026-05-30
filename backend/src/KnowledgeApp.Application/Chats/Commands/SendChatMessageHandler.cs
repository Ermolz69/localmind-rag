using KnowledgeApp.Application.Abstractions;
using KnowledgeApp.Application.Common.Errors;
using KnowledgeApp.Application.Common.Results;
using KnowledgeApp.Contracts.Rag;
using KnowledgeApp.Domain.Entities;
using KnowledgeApp.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeApp.Application.Chats;

public sealed class SendChatMessageHandler(
    IConversationRepository conversationRepository,
    IUnitOfWork unitOfWork,
    IRagAnswerGenerator ragAnswerGenerator,
    ChatRequestValidator validator,
    IDateTimeProvider dateTimeProvider,
    ILocalDeviceResolver localDeviceResolver)
{
    public async Task<Result<RagAnswerDto>> HandleAsync(
        Guid conversationId,
        ChatMessageRequest request,
        CancellationToken cancellationToken = default)
    {
        Result validation = validator.Validate(request);
        if (!validation.IsSuccess)
        {
            return Result<RagAnswerDto>.Failure(validation);
        }

        bool conversationExists = await conversationRepository.ExistsAsync(conversationId, cancellationToken);
        if (!conversationExists)
        {
            return Result<RagAnswerDto>.Failure(ApplicationErrors.NotFound(ErrorCodes.Chats.NotFound, ErrorMessages.Chats.NotFound));
        }

        DateTimeOffset now = dateTimeProvider.UtcNow;
        Guid localDeviceId = await localDeviceResolver.ResolveCurrentDeviceIdAsync(cancellationToken);
        await conversationRepository.AddMessageAsync(new ChatMessage
        {
            ConversationId = conversationId,
            CreatedAt = now,
            LocalDeviceId = localDeviceId,
            Role = ChatRole.User,
            Content = request.Content.Trim(),
        }, cancellationToken);

        RagAnswerDto answer = await ragAnswerGenerator.AnswerAsync(conversationId, request.Content.Trim(), cancellationToken);

        await conversationRepository.AddMessageAsync(new ChatMessage
        {
            ConversationId = conversationId,
            CreatedAt = now,
            LocalDeviceId = localDeviceId,
            Role = ChatRole.Assistant,
            Content = answer.Answer,
        }, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<RagAnswerDto>.Success(answer);
    }
}
