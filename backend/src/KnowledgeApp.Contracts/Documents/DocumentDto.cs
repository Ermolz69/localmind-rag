namespace KnowledgeApp.Contracts.Documents;

/// <summary>Document metadata returned by document endpoints.</summary>
/// <param name="Id">Local document identifier.</param>
/// <param name="Name">Original or display document name.</param>
/// <param name="Status">Current ingestion/indexing status.</param>
/// <param name="CreatedAt">UTC creation timestamp.</param>
/// <param name="LastError">Latest ingestion error message, when indexing failed.</param>
/// <param name="Tags">Metadata tags attached to the document.</param>
public sealed record DocumentDto(Guid Id, string Name, string Status, DateTimeOffset CreatedAt, string? LastError = null, IReadOnlyDictionary<string, string>? Tags = null);

