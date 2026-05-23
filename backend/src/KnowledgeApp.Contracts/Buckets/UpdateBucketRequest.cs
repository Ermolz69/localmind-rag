namespace KnowledgeApp.Contracts.Buckets;

/// <summary>Request used to update an existing local bucket.</summary>
/// <param name="Name">Updated bucket name.</param>
/// <param name="Description">Updated optional bucket description.</param>
public sealed record UpdateBucketRequest(string Name, string? Description);
