using KnowledgeApp.Domain.Common;

namespace KnowledgeApp.Domain.Entities;

/// <summary>
/// A phone paired as a trusted Companion Mode device. Persisted so it survives
/// app restarts and can reconnect without re-pairing. Only the token's hash is
/// stored, never the token itself.
/// </summary>
public sealed class CompanionDevice : Entity
{
    public string Name { get; set; } = string.Empty;

    public string Platform { get; set; } = string.Empty;

    /// <summary>SHA-256 hash (hex) of the device's authentication token.</summary>
    public string TokenHash { get; set; } = string.Empty;

    public DateTimeOffset LastSeenAt { get; set; }

    public bool CanChat { get; set; }

    public bool CanSearch { get; set; }

    public bool CanViewDocuments { get; set; }

    public bool CanViewStatus { get; set; }

    public bool CanRescan { get; set; }

    public bool CanAddFiles { get; set; }
}
