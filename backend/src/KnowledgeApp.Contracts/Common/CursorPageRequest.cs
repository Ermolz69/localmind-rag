namespace KnowledgeApp.Contracts.Common;

public sealed record CursorPageRequest(string? Cursor = null, int Limit = 50);
