namespace KnowledgeApp.Contracts.Common;

/// <summary>Cursor pagination parameters accepted by list endpoints.</summary>
/// <param name="Cursor">Opaque cursor returned by a previous page.</param>
/// <param name="Limit">Requested page size.</param>
public sealed record CursorPageRequest(string? Cursor = null, int Limit = 50);
