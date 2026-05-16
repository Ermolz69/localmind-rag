namespace KnowledgeApp.Contracts.Rag;

public sealed record SemanticSearchRequest(string Query, int Limit = 8, Guid? BucketId = null, Guid? DocumentId = null);


