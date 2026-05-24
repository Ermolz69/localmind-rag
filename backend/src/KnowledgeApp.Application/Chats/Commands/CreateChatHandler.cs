using KnowledgeApp.Application.Abstractions;
using KnowledgeApp.Application.Common.Results;
using KnowledgeApp.Contracts.Chats;
using KnowledgeApp.Domain.Entities;

namespace KnowledgeApp.Application.Chats;

public sealed class CreateChatHandler(
    IAppDbContext dbContext,
    ChatRequestValidator validator,
    ILocalDeviceResolver localDeviceResolver)
{
    public async Task<Result<ConversationDto>> HandleAsync(
        CreateConversationRequest request,
        CancellationToken cancellationToken = default)
    {
        Result validation = validator.Validate(request);
        if (!validation.IsSuccess)
        {
            return Result<ConversationDto>.Failure(validation);
        }

        Guid localDeviceId = await localDeviceResolver.ResolveCurrentDeviceIdAsync(cancellationToken);
        Conversation conversation = new()
        {
            Title = request.Title.Trim(),
            LocalDeviceId = localDeviceId,
        };

        dbContext.Conversations.Add(conversation);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result<ConversationDto>.Success(ConversationMapper.ToDto(conversation));
    }
}
