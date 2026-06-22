using KnowledgeApp.Contracts.Companion;

namespace KnowledgeApp.LocalApi.CompanionGateway;

/// <summary>Capabilities a phone can be granted (mirrors <see cref="CompanionDevicePermissionsDto"/>).</summary>
public enum CompanionCapability
{
    Chat,
    Search,
    ViewDocuments,
    ViewStatus,
    Rescan,
    AddFiles,
}

/// <summary>How the gateway treats a request path.</summary>
public enum CompanionRouteAccess
{
    /// <summary>Not exposed to phones — return 404.</summary>
    Blocked,

    /// <summary>Allowed without a device token (pairing bootstrap).</summary>
    Bootstrap,

    /// <summary>Allowed only for a trusted device that holds the capability.</summary>
    RequiresCapability,
}

/// <summary>The decision for a single request.</summary>
public readonly record struct CompanionRouteDecision(
    CompanionRouteAccess Access,
    CompanionCapability Capability)
{
    public static CompanionRouteDecision Blocked { get; } = new(CompanionRouteAccess.Blocked, default);

    public static CompanionRouteDecision Bootstrap { get; } = new(CompanionRouteAccess.Bootstrap, default);

    public static CompanionRouteDecision Requires(CompanionCapability capability) =>
        new(CompanionRouteAccess.RequiresCapability, capability);
}

/// <summary>
/// The allowlist that decides which loopback API routes the LAN gateway exposes to
/// a phone and which capability each one needs. Anything not listed is blocked, so
/// dangerous routes (settings, runtime, document upload/delete/reindex, buckets,
/// notes, device/pairing management) are never reachable from a phone.
/// </summary>
public static class CompanionRoutePolicy
{
    public static CompanionRouteDecision Evaluate(string path, string method)
    {
        string p = (path ?? string.Empty).TrimEnd('/').ToLowerInvariant();
        bool isGet = HttpMethods.IsGet(method);

        // Pairing bootstrap (phone has no token yet).
        if (p == "/api/v1/companion/pairing/confirm" && HttpMethods.IsPost(method))
        {
            return CompanionRouteDecision.Bootstrap;
        }

        if (p == "/api/v1/companion/info" && isGet)
        {
            return CompanionRouteDecision.Bootstrap;
        }

        if (p == "/api/v1/companion/activity" && isGet)
        {
            return CompanionRouteDecision.Requires(CompanionCapability.ViewStatus);
        }

        // File picking from allowed folders.
        if (p == "/api/v1/companion/files/roots" && isGet)
        {
            return CompanionRouteDecision.Requires(CompanionCapability.AddFiles);
        }

        if (p == "/api/v1/companion/files/browse" && isGet)
        {
            return CompanionRouteDecision.Requires(CompanionCapability.AddFiles);
        }

        if (p == "/api/v1/companion/files/add" && HttpMethods.IsPost(method))
        {
            return CompanionRouteDecision.Requires(CompanionCapability.AddFiles);
        }

        // Chat (list/create/messages/stream/title) — all methods under /chats.
        if (IsUnder(p, "/api/v1/chats"))
        {
            return CompanionRouteDecision.Requires(CompanionCapability.Chat);
        }

        // Semantic + content search.
        if (IsUnder(p, "/api/v1/search"))
        {
            return CompanionRouteDecision.Requires(CompanionCapability.Search);
        }

        // Documents — read only (GET list + preview); writes stay blocked.
        if (IsUnder(p, "/api/v1/documents") && isGet)
        {
            return CompanionRouteDecision.Requires(CompanionCapability.ViewDocuments);
        }

        // Ingestion — read only (job list/status); job actions stay blocked for now.
        if (IsUnder(p, "/api/v1/ingestion") && isGet)
        {
            return CompanionRouteDecision.Requires(CompanionCapability.ViewStatus);
        }

        // Watched folders — status is read, rescan/cleanup are actions.
        if (p == "/api/v1/watched-folders/status" && isGet)
        {
            return CompanionRouteDecision.Requires(CompanionCapability.ViewStatus);
        }

        if ((p == "/api/v1/watched-folders/rescan" || p == "/api/v1/watched-folders/cleanup")
            && HttpMethods.IsPost(method))
        {
            return CompanionRouteDecision.Requires(CompanionCapability.Rescan);
        }

        return CompanionRouteDecision.Blocked;
    }

    public static bool HasCapability(CompanionDevicePermissionsDto permissions, CompanionCapability capability) =>
        capability switch
        {
            CompanionCapability.Chat => permissions.Chat,
            CompanionCapability.Search => permissions.Search,
            CompanionCapability.ViewDocuments => permissions.ViewDocuments,
            CompanionCapability.ViewStatus => permissions.ViewStatus,
            CompanionCapability.Rescan => permissions.Rescan,
            CompanionCapability.AddFiles => permissions.AddFiles,
            _ => false,
        };

    private static bool IsUnder(string path, string prefix) =>
        path == prefix || path.StartsWith(prefix + "/", StringComparison.Ordinal);
}
