using KnowledgeApp.Contracts.Companion;
using KnowledgeApp.LocalApi.CompanionGateway;

namespace KnowledgeApp.UnitTests.Companion;

public sealed class CompanionRoutePolicyTests
{
    [Theory]
    [InlineData("/api/v1/companion/pairing/confirm", "POST")]
    [InlineData("/api/v1/companion/info", "GET")]
    public void Evaluate_Allows_Bootstrap_Routes_Without_Auth(string path, string method)
    {
        Assert.Equal(CompanionRouteAccess.Bootstrap, CompanionRoutePolicy.Evaluate(path, method).Access);
    }

    [Theory]
    [InlineData("/api/v1/chats", "GET", CompanionCapability.Chat)]
    [InlineData("/api/v1/chats/abc/messages/stream", "POST", CompanionCapability.Chat)]
    [InlineData("/api/v1/search/semantic", "POST", CompanionCapability.Search)]
    [InlineData("/api/v1/documents", "GET", CompanionCapability.ViewDocuments)]
    [InlineData("/api/v1/documents/abc/preview", "GET", CompanionCapability.ViewDocuments)]
    [InlineData("/api/v1/ingestion/jobs", "GET", CompanionCapability.ViewStatus)]
    [InlineData("/api/v1/watched-folders/status", "GET", CompanionCapability.ViewStatus)]
    [InlineData("/api/v1/watched-folders/rescan", "POST", CompanionCapability.Rescan)]
    [InlineData("/api/v1/watched-folders/cleanup", "POST", CompanionCapability.Rescan)]
    [InlineData("/api/v1/companion/files/roots", "GET", CompanionCapability.AddFiles)]
    [InlineData("/api/v1/companion/files/add", "POST", CompanionCapability.AddFiles)]
    [InlineData("/api/v1/companion/activity", "GET", CompanionCapability.ViewStatus)]
    public void Evaluate_Maps_Allowlisted_Routes_To_Capability(
        string path,
        string method,
        CompanionCapability expected)
    {
        CompanionRouteDecision decision = CompanionRoutePolicy.Evaluate(path, method);

        Assert.Equal(CompanionRouteAccess.RequiresCapability, decision.Access);
        Assert.Equal(expected, decision.Capability);
    }

    [Theory]
    [InlineData("/api/v1/documents", "POST")] // upload
    [InlineData("/api/v1/documents/abc", "DELETE")] // delete
    [InlineData("/api/v1/documents/abc/reindex", "POST")] // reindex
    [InlineData("/api/v1/ingestion/jobs/abc/cancel", "POST")] // job action
    [InlineData("/api/v1/settings", "GET")]
    [InlineData("/api/v1/settings", "PUT")]
    [InlineData("/api/v1/runtime/status", "GET")]
    [InlineData("/api/v1/buckets", "POST")]
    [InlineData("/api/v1/companion/devices", "GET")] // device management is desktop-only
    [InlineData("/api/v1/companion/pairing", "POST")] // starting pairing is desktop-only
    [InlineData("/api/v1/notes", "GET")]
    public void Evaluate_Blocks_Everything_Outside_The_Allowlist(string path, string method)
    {
        Assert.Equal(CompanionRouteAccess.Blocked, CompanionRoutePolicy.Evaluate(path, method).Access);
    }

    [Fact]
    public void HasCapability_Maps_Each_Capability_To_Its_Permission()
    {
        CompanionDevicePermissionsDto onlyChat = new(
            Chat: true,
            Search: false,
            ViewDocuments: false,
            ViewStatus: false,
            Rescan: false,
            AddFiles: false);

        Assert.True(CompanionRoutePolicy.HasCapability(onlyChat, CompanionCapability.Chat));
        Assert.False(CompanionRoutePolicy.HasCapability(onlyChat, CompanionCapability.Search));
        Assert.False(CompanionRoutePolicy.HasCapability(onlyChat, CompanionCapability.AddFiles));
    }
}
