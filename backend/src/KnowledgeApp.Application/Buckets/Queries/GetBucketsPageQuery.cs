namespace KnowledgeApp.Application.Buckets;

public sealed record GetBucketsPageQuery(string? Query, string? Cursor, int Limit);
