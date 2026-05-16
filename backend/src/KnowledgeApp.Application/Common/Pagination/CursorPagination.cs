using System.Text;
using System.Text.Json;
using KnowledgeApp.Application.Exceptions;
using KnowledgeApp.Contracts.Common;

namespace KnowledgeApp.Application.Common.Pagination;

public sealed record CursorPageOptions(string? Cursor, int Limit);

public sealed record CursorPayload(
    string Kind,
    string FilterHash,
    DateTimeOffset? PrimaryDate,
    DateTimeOffset CreatedAt,
    Guid Id,
    bool HasPrimaryDate);

public static class CursorPagination
{
    public const int DefaultLimit = 50;
    public const int MaxLimit = 200;

    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public static int ValidateLimit(int limit)
    {
        if (limit < 1 || limit > MaxLimit)
        {
            throw new ValidationAppException(
                "pagination.invalidLimit",
                "Cursor page limit must be between 1 and 200.",
                new Dictionary<string, string[]> { ["limit"] = ["Limit must be between 1 and 200."] });
        }

        return limit;
    }

    public static string CreateFilterHash(object filters)
    {
        string json = JsonSerializer.Serialize(filters, SerializerOptions);
        byte[] bytes = Encoding.UTF8.GetBytes(json);
        return Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(bytes));
    }

    public static CursorPayload? Decode(string? cursor, string kind, string filterHash)
    {
        if (string.IsNullOrWhiteSpace(cursor))
        {
            return null;
        }

        try
        {
            string normalized = cursor.Replace('-', '+').Replace('_', '/');
            int padding = (4 - (normalized.Length % 4)) % 4;
            normalized = normalized.PadRight(normalized.Length + padding, '=');
            byte[] bytes = Convert.FromBase64String(normalized);
            CursorPayload? payload = JsonSerializer.Deserialize<CursorPayload>(bytes, SerializerOptions);

            if (payload is null || payload.Kind != kind || payload.FilterHash != filterHash)
            {
                throw CreateInvalidCursorException();
            }

            return payload;
        }
        catch (FormatException exception)
        {
            throw CreateInvalidCursorException(exception);
        }
        catch (JsonException exception)
        {
            throw CreateInvalidCursorException(exception);
        }
    }

    public static string Encode(CursorPayload payload)
    {
        byte[] bytes = JsonSerializer.SerializeToUtf8Bytes(payload, SerializerOptions);
        return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    public static CursorPage<T> CreatePage<T>(
        IReadOnlyList<T> sortedItems,
        CursorPayload? cursor,
        int limit,
        Func<T, CursorPayload, int> compareToCursor,
        Func<T, CursorPayload> createCursor)
    {
        int startIndex = 0;
        if (cursor is not null)
        {
            startIndex = FindStartIndex(sortedItems, cursor, compareToCursor);
        }

        List<T> pageItems = sortedItems.Skip(startIndex).Take(limit + 1).ToList();
        bool hasMore = pageItems.Count > limit;
        if (hasMore)
        {
            pageItems.RemoveAt(pageItems.Count - 1);
        }

        string? nextCursor = hasMore && pageItems.Count > 0
            ? Encode(createCursor(pageItems[^1]))
            : null;

        return new CursorPage<T>(pageItems, nextCursor, limit, hasMore);
    }

    private static int FindStartIndex<T>(
        IReadOnlyList<T> sortedItems,
        CursorPayload cursor,
        Func<T, CursorPayload, int> compareToCursor)
    {
        for (int index = 0; index < sortedItems.Count; index++)
        {
            int comparison = compareToCursor(sortedItems[index], cursor);
            if (comparison == 2)
            {
                return index + 1;
            }

            if (comparison == 1)
            {
                return index;
            }
        }

        return sortedItems.Count;
    }

    private static ValidationAppException CreateInvalidCursorException(Exception? exception = null)
    {
        return new ValidationAppException(
            "pagination.invalidCursor",
            exception is null ? "Cursor is invalid for this request." : $"Cursor is invalid for this request: {exception.Message}",
            new Dictionary<string, string[]> { ["cursor"] = ["Cursor is invalid for this request."] });
    }
}
