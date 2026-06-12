using System.Runtime.CompilerServices;
using System.Text;

using KnowledgeApp.Application.Abstractions;
using KnowledgeApp.Application.Common.Errors;
using KnowledgeApp.Application.Common.Results;
using KnowledgeApp.Application.Exceptions;
using KnowledgeApp.Contracts.Rag;
using KnowledgeApp.Domain.Entities;
using KnowledgeApp.Domain.Enums;

using Microsoft.EntityFrameworkCore;

using KnowledgeApp.Application.Common.Diagnostics;

namespace KnowledgeApp.Application.Chats;

public sealed class SendChatStreamMessageHandler(
    IConversationRepository conversationRepository,
    IUnitOfWork unitOfWork,
    IRagAnswerGenerator ragAnswerGenerator,
    ChatRequestValidator validator,
    IDateTimeProvider dateTimeProvider,
    ILocalDeviceResolver localDeviceResolver,
    IOperationLogRepository operationLogRepository)
{
    public async Task ValidateAndPrepareAsync(
        Guid conversationId,
        ChatMessageRequest request,
        CancellationToken cancellationToken = default)
    {
        Result validation = validator.Validate(request);
        if (!validation.IsSuccess)
        {
            var errorsDict = validation.Error!.Details?
                .GroupBy(d => d.Field)
                .ToDictionary(
                    g => g.Key ?? string.Empty,
                    g => g.Select(d => d.Message).ToArray()
                ) ?? new Dictionary<string, string[]>();

            throw new ValidationAppException(validation.Error.Code, validation.Error.Message, errorsDict);
        }

        bool conversationExists = await conversationRepository.ExistsAsync(conversationId, cancellationToken);
        if (!conversationExists)
        {
            var error = ApplicationErrors.NotFound(ErrorCodes.Chats.NotFound, ErrorMessages.Chats.NotFound);
            throw new NotFoundAppException(error.Code, error.Message);
        }
    }

    public async IAsyncEnumerable<RagAnswerChunkDto> HandleStreamAsync(
        Guid conversationId,
        ChatMessageRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
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

        await operationLogRepository.AddAsync(new OperationLog
        {
            OperationType = "Chat.Ask",
            EntityType = "Conversation",
            EntityId = conversationId.ToString(),
            Message = "User asked a question in chat",
            MetadataJson = "{}"
        }, cancellationToken);

        // Save the user message first so it appears in the conversation history
        await unitOfWork.SaveChangesAsync(cancellationToken);

        StringBuilder fullAnswer = new();

        try
        {
            await foreach (RagAnswerChunkDto chunk in ragAnswerGenerator.AnswerStreamAsync(conversationId, request.Content.Trim(), request.Filters, cancellationToken))
            {
                fullAnswer.Append(chunk.Text);
                yield return chunk;
            }
        }
        finally
        {
            // Even if the client cancels, we try to save the partial answer to the database
            if (fullAnswer.Length > 0)
            {
                await conversationRepository.AddMessageAsync(new ChatMessage
                {
                    ConversationId = conversationId,
                    CreatedAt = dateTimeProvider.UtcNow,
                    LocalDeviceId = localDeviceId,
                    Role = ChatRole.Assistant,
                    Content = fullAnswer.ToString(),
                }, CancellationToken.None);

                // Use CancellationToken.None to ensure the assistant message is saved even if the request is cancelled
                await unitOfWork.SaveChangesAsync(CancellationToken.None);
            }
        }
    }
}
