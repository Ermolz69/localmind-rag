using KnowledgeApp.Application.Abstractions;
using KnowledgeApp.Application.Common.Errors;
using KnowledgeApp.Application.Common.Results;
using KnowledgeApp.Contracts.Chats;
using KnowledgeApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeApp.Application.Chats;

public sealed class GetConversationByIdHandler(IConversationRepository conversationRepository)
{
    public async Task<Result<ConversationDto>> HandleAsync(Guid conversationId, CancellationToken cancellationToken = default)
    {
        Conversation? conversation = await conversationRepository.GetByIdAsync(conversationId, cancellationToken);

        return conversation is null
            ? Result<ConversationDto>.Failure(ApplicationErrors.NotFound(ErrorCodes.Chats.NotFound, ErrorMessages.Chats.NotFound))
            : Result<ConversationDto>.Success(ConversationMapper.ToDto(conversation));
    }
}
