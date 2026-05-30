using KnowledgeApp.Application.Abstractions;
using KnowledgeApp.Application.Common.Errors;
using KnowledgeApp.Application.Common.Results;
using KnowledgeApp.Contracts.Chats;
using KnowledgeApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeApp.Application.Chats;

public sealed class GetChatMessagesHandler(IConversationRepository conversationRepository)
{
    public async Task<Result<IReadOnlyList<ChatMessageDto>>> HandleAsync(
        Guid conversationId,
        CancellationToken cancellationToken = default)
    {
        bool conversationExists = await conversationRepository.ExistsAsync(conversationId, cancellationToken);
        if (!conversationExists)
        {
            return Result<IReadOnlyList<ChatMessageDto>>.Failure(
                ApplicationErrors.NotFound(ErrorCodes.Chats.NotFound, ErrorMessages.Chats.NotFound));
        }

        IReadOnlyList<ChatMessage> messages = await conversationRepository.GetMessagesAsync(conversationId, cancellationToken);

        IReadOnlyList<ChatMessageDto> result = messages
            .OrderBy(message => message.CreatedAt)
            .ThenBy(message => message.Id.ToString("N", System.Globalization.CultureInfo.InvariantCulture))
            .Select(ConversationMapper.ToDto)
            .ToArray();

        return Result<IReadOnlyList<ChatMessageDto>>.Success(result);
    }
}
