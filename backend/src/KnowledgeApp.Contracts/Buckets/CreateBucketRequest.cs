namespace KnowledgeApp.Contracts.Buckets;

/// <summary>Request used to create a local bucket.</summary>
/// <param name="Name">Required bucket name.</param>
/// <param name="Description">Optional bucket description.</param>
public sealed record CreateBucketRequest(string Name, string? Description);
