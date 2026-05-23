namespace KnowledgeApp.Contracts.Rag;

/// <summary>Request used to search indexed document chunks semantically.</summary>
/// <param name="Query">Natural-language search query.</param>
/// <param name="Limit">Maximum number of sources to return.</param>
/// <param name="BucketId">Optional bucket scope for the search.</param>
/// <param name="DocumentId">Optional document scope for the search.</param>
public sealed record SemanticSearchRequest(string Query, int Limit = 8, Guid? BucketId = null, Guid? DocumentId = null);
