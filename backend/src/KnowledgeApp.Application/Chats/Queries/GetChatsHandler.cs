using System.Globalization;
using KnowledgeApp.Application.Abstractions;
using KnowledgeApp.Application.Common.Pagination;
using KnowledgeApp.Application.Common.Results;
using KnowledgeApp.Contracts.Common;
using KnowledgeApp.Contracts.Chats;
using KnowledgeApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeApp.Application.Chats;

public sealed class GetChatsHandler(IConversationRepository conversationRepository)
{
    private const string CursorKind = "chats";

    public async Task<Result<CursorPage<ConversationDto>>> HandleAsync(
        GetChatsQuery query,
        CancellationToken cancellationToken = default)
    {
        Result<int> limitResult = CursorPagination.ValidateLimit(query.Limit);
        if (!limitResult.IsSuccess)
        {
            return Result<CursorPage<ConversationDto>>.Failure(limitResult.Error!);
        }

        int limit = limitResult.Value;
        string filterHash = CursorPagination.CreateFilterHash(new { });
        Result<CursorPayload?> cursorResult = CursorPagination.Decode(query.Cursor, CursorKind, filterHash);
        if (!cursorResult.IsSuccess)
        {
            return Result<CursorPage<ConversationDto>>.Failure(cursorResult.Error!);
        }

        CursorPayload? cursor = cursorResult.Value;

        IReadOnlyList<Conversation> conversations = await conversationRepository.ListAsync(cancellationToken);
        Conversation[] sortedConversations = conversations
            .OrderByDescending(conversation => conversation.UpdatedAt.HasValue)
            .ThenByDescending(conversation => conversation.UpdatedAt)
            .ThenByDescending(conversation => conversation.CreatedAt)
            .ThenByDescending(conversation => conversation.Id.ToString("N", CultureInfo.InvariantCulture))
            .ToArray();
        CursorPage<Conversation> conversationPage = CursorPagination.CreatePage(
            sortedConversations,
            cursor,
            limit,
            CompareConversationToCursor,
            conversation => new CursorPayload(
                CursorKind,
                filterHash,
                conversation.UpdatedAt,
                conversation.CreatedAt,
                conversation.Id,
                conversation.UpdatedAt.HasValue));
        ConversationDto[] conversationDtos = conversationPage.Items.Select(ConversationMapper.ToDto).ToArray();

        return Result<CursorPage<ConversationDto>>.Success(new CursorPage<ConversationDto>(
            conversationDtos,
            conversationPage.NextCursor,
            conversationPage.Limit,
            conversationPage.HasMore));
    }

    private static int CompareConversationToCursor(Conversation conversation, CursorPayload cursor)
    {
        if (conversation.Id == cursor.Id)
        {
            return 2;
        }

        bool hasUpdatedAt = conversation.UpdatedAt.HasValue;
        if (!hasUpdatedAt && cursor.HasPrimaryDate)
        {
            return 1;
        }

        if (hasUpdatedAt == cursor.HasPrimaryDate)
        {
            DateTimeOffset? updatedAt = conversation.UpdatedAt;
            if (updatedAt < cursor.PrimaryDate)
            {
                return 1;
            }

            if (updatedAt == cursor.PrimaryDate)
            {
                if (conversation.CreatedAt < cursor.CreatedAt)
                {
                    return 1;
                }

                if (conversation.CreatedAt == cursor.CreatedAt &&
                    string.Compare(
                        conversation.Id.ToString("N", CultureInfo.InvariantCulture),
                        cursor.Id.ToString("N", CultureInfo.InvariantCulture),
                        StringComparison.Ordinal) < 0)
                {
                    return 1;
                }
            }
        }

        return 0;
    }
}
