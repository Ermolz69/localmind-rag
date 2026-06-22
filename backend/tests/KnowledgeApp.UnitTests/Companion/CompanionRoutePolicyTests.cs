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
    [InlineData("/api/v1/documents/abc", "PUT")] // edit
    [InlineData("/api/v1/documents/abc/reindex", "POST")] // reindex
    [InlineData("/api/v1/documents/abc/preview", "DELETE")] // read-only surface, wrong method
    [InlineData("/api/v1/ingestion/jobs/abc/cancel", "POST")] // job action
    [InlineData("/api/v1/settings", "GET")] // critical settings / system paths
    [InlineData("/api/v1/settings", "PUT")]
    [InlineData("/api/v1/runtime/status", "GET")] // AI runtime
    [InlineData("/api/v1/runtime/restart", "POST")]
    [InlineData("/api/v1/buckets", "GET")]
    [InlineData("/api/v1/buckets", "POST")]
    [InlineData("/api/v1/companion/devices", "GET")] // device management is desktop-only
    [InlineData("/api/v1/companion/devices/abc", "DELETE")]
    [InlineData("/api/v1/companion/devices/abc/permissions", "PUT")] // a phone can't change its own grants
    [InlineData("/api/v1/companion/pairing", "POST")] // starting pairing is desktop-only
    [InlineData("/api/v1/companion/pairing", "GET")]
    [InlineData("/api/v1/companion/pairing", "DELETE")]
    [InlineData("/api/v1/watched-folders/rescan", "GET")] // action route, wrong method
    [InlineData("/api/v1/companion/files/add", "GET")]
    [InlineData("/api/v1/notes", "GET")]
    [InlineData("/api/v1/unknown", "GET")]
    public void Evaluate_Blocks_Everything_Outside_The_Allowlist(string path, string method)
    {
        Assert.Equal(CompanionRouteAccess.Blocked, CompanionRoutePolicy.Evaluate(path, method).Access);
    }

    [Theory]
    [InlineData("/API/V1/DOCUMENTS", "GET", CompanionCapability.ViewDocuments)]
    [InlineData("/api/v1/documents/", "GET", CompanionCapability.ViewDocuments)]
    [InlineData("/api/v1/chats/", "get", CompanionCapability.Chat)]
    public void Evaluate_Is_Case_And_Trailing_Slash_Insensitive(
        string path,
        string method,
        CompanionCapability expected)
    {
        CompanionRouteDecision decision = CompanionRoutePolicy.Evaluate(path, method);

        Assert.Equal(CompanionRouteAccess.RequiresCapability, decision.Access);
        Assert.Equal(expected, decision.Capability);
    }

    [Theory]
    [InlineData(CompanionCapability.Chat)]
    [InlineData(CompanionCapability.Search)]
    [InlineData(CompanionCapability.ViewDocuments)]
    [InlineData(CompanionCapability.ViewStatus)]
    [InlineData(CompanionCapability.Rescan)]
    [InlineData(CompanionCapability.AddFiles)]
    public void HasCapability_Is_True_Only_For_The_Granted_Capability(CompanionCapability granted)
    {
        CompanionDevicePermissionsDto permissions = Only(granted);

        foreach (CompanionCapability capability in Enum.GetValues<CompanionCapability>())
        {
            Assert.Equal(capability == granted, CompanionRoutePolicy.HasCapability(permissions, capability));
        }
    }

    [Fact]
    public void HasCapability_Is_False_For_Everything_When_Nothing_Is_Granted()
    {
        CompanionDevicePermissionsDto none = new(false, false, false, false, false, false);

        foreach (CompanionCapability capability in Enum.GetValues<CompanionCapability>())
        {
            Assert.False(CompanionRoutePolicy.HasCapability(none, capability));
        }
    }

    private static CompanionDevicePermissionsDto Only(CompanionCapability capability) => new(
        Chat: capability == CompanionCapability.Chat,
        Search: capability == CompanionCapability.Search,
        ViewDocuments: capability == CompanionCapability.ViewDocuments,
        ViewStatus: capability == CompanionCapability.ViewStatus,
        Rescan: capability == CompanionCapability.Rescan,
        AddFiles: capability == CompanionCapability.AddFiles);
}
