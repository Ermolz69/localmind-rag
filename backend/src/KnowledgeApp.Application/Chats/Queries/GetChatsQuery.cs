namespace KnowledgeApp.Application.Chats;

public sealed record GetChatsQuery(string? Cursor = null, int Limit = 50);
