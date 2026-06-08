namespace KnowledgeApp.Application.Abstractions.Ingestion;

public readonly record struct TokenSpan(int StartIndex, int Length, int TokenCount);
