namespace KnowledgeApp.Contracts.Common;

/// <summary>Cursor-paged API response.</summary>
/// <typeparam name="T">Item type contained in the page.</typeparam>
/// <param name="Items">Items returned for the current page.</param>
/// <param name="NextCursor">Cursor to pass to the next request, or null when there is no next page.</param>
/// <param name="Limit">Maximum number of items requested for this page.</param>
/// <param name="HasMore">True when another page can be requested.</param>
public sealed record CursorPage<T>(IReadOnlyList<T> Items, string? NextCursor, int Limit, bool HasMore);
