using KnowledgeApp.Application.Abstractions;
using KnowledgeApp.Contracts.Chats;
using KnowledgeApp.Domain.Entities;

namespace KnowledgeApp.Application.Chats;

public sealed class CreateChatHandler(IAppDbContext dbContext, ChatRequestValidator validator)
{
    public async Task<ConversationDto> HandleAsync(
        CreateConversationRequest request,
        CancellationToken cancellationToken = default)
    {
        validator.Validate(request);

        Conversation conversation = new()
        {
            Title = request.Title.Trim(),
        };

        dbContext.Conversations.Add(conversation);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ConversationMapper.ToDto(conversation);
    }
}
