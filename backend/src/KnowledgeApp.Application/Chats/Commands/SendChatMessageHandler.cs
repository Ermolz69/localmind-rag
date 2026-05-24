using KnowledgeApp.Application.Abstractions;
using KnowledgeApp.Application.Common.Errors;
using KnowledgeApp.Application.Common.Results;
using KnowledgeApp.Contracts.Rag;
using KnowledgeApp.Domain.Entities;
using KnowledgeApp.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeApp.Application.Chats;

public sealed class SendChatMessageHandler(
    IAppDbContext dbContext,
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

        bool conversationExists = await dbContext.Conversations
            .AsNoTracking()
            .AnyAsync(conversation => conversation.Id == conversationId && conversation.DeletedAt == null, cancellationToken);
        if (!conversationExists)
        {
            return Result<RagAnswerDto>.Failure(ApplicationErrors.NotFound(ErrorCodes.Chats.NotFound, ErrorMessages.Chats.NotFound));
        }

        DateTimeOffset now = dateTimeProvider.UtcNow;
        Guid localDeviceId = await localDeviceResolver.ResolveCurrentDeviceIdAsync(cancellationToken);
        dbContext.ChatMessages.Add(new ChatMessage
        {
            ConversationId = conversationId,
            CreatedAt = now,
            LocalDeviceId = localDeviceId,
            Role = ChatRole.User,
            Content = request.Content.Trim(),
        });

        RagAnswerDto answer = await ragAnswerGenerator.AnswerAsync(conversationId, request.Content.Trim(), cancellationToken);

        dbContext.ChatMessages.Add(new ChatMessage
        {
            ConversationId = conversationId,
            CreatedAt = now,
            LocalDeviceId = localDeviceId,
            Role = ChatRole.Assistant,
            Content = answer.Answer,
        });

        await dbContext.SaveChangesAsync(cancellationToken);
        return Result<RagAnswerDto>.Success(answer);
    }
}
