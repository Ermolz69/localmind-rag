using KnowledgeApp.Contracts.Companion;

namespace KnowledgeApp.Application.Abstractions;

/// <summary>
/// A small in-memory feed of recent activity (document ingestion, watched-folder
/// finds, device connect/disconnect) the phone can poll to see what LocalMind is
/// doing in near real time. Publishing is best-effort and must never throw into
/// callers on the main flows.
/// </summary>
public interface ICompanionActivityFeed
{
    /// <summary>Records an activity event.</summary>
    void Publish(string kind, string message, string? detail = null);

    /// <summary>Returns the most recent events, newest first.</summary>
    IReadOnlyList<CompanionActivityEventDto> GetRecent(int limit);
}
