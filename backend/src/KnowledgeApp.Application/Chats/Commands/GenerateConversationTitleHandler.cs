using KnowledgeApp.Application.Abstractions;
using KnowledgeApp.Application.Common.Errors;
using KnowledgeApp.Application.Common.Results;
using KnowledgeApp.Contracts.Chats;
using KnowledgeApp.Domain.Entities;

namespace KnowledgeApp.Application.Chats;

public sealed class GenerateConversationTitleHandler(
    IConversationRepository conversationRepository,
    IChatTitleGenerator titleGenerator,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider)
{
    private const int MaxTitleLength = 60;
    private const string DefaultTitle = "New chat";

    public async Task<Result<ConversationDto>> HandleAsync(
        Guid conversationId,
        CancellationToken cancellationToken = default)
    {
        Conversation? conversation = await conversationRepository.GetByIdAsync(conversationId, cancellationToken);
        if (conversation is null)
        {
            return Result<ConversationDto>.Failure(ApplicationErrors.NotFound(ErrorCodes.Chats.NotFound, ErrorMessages.Chats.NotFound));
        }

        if (conversation.TitleEditedAt is not null || conversation.TitleGeneratedAt is not null)
        {
            return Result<ConversationDto>.Success(ConversationMapper.ToDto(conversation));
        }

        ChatMessage? firstUserMessage =
            await conversationRepository.GetFirstUserMessageAsync(conversationId, cancellationToken);
        if (firstUserMessage is null)
        {
            return Result<ConversationDto>.Success(ConversationMapper.ToDto(conversation));
        }

        string generatedTitle;
        try
        {
            generatedTitle = await titleGenerator.GenerateAsync(firstUserMessage.Content, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            generatedTitle = string.Empty;
        }

        DateTimeOffset now = dateTimeProvider.UtcNow;
        conversation.Title = NormalizeTitle(generatedTitle, firstUserMessage.Content);
        conversation.TitleGeneratedAt = now;
        conversation.UpdatedAt = now;

        await conversationRepository.UpdateAsync(conversation, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<ConversationDto>.Success(ConversationMapper.ToDto(conversation));
    }

    private static string NormalizeTitle(string generatedTitle, string fallbackMessage)
    {
        string title = NormalizeLine(generatedTitle);
        if (string.IsNullOrWhiteSpace(title) || title.Length > MaxTitleLength)
        {
            title = NormalizeLine(fallbackMessage);
        }

        if (title.Length > MaxTitleLength)
        {
            title = title[..MaxTitleLength].Trim();
        }

        title = title.Trim().Trim('"', '\'').TrimEnd('.').Trim();
        return string.IsNullOrWhiteSpace(title) ? DefaultTitle : title;
    }

    private static string NormalizeLine(string value)
    {
        string firstLine = value
            .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault() ?? string.Empty;

        return string.Join(' ', firstLine.Split([' ', '\t'], StringSplitOptions.RemoveEmptyEntries));
    }
}
