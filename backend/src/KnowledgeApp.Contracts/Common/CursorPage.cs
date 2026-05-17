namespace KnowledgeApp.Contracts.Common;

public sealed record CursorPage<T>(IReadOnlyList<T> Items, string? NextCursor, int Limit, bool HasMore);
