namespace KnowledgeApp.LocalApi.CompanionGateway;

/// <summary>Configuration for the LAN-facing Companion Gateway.</summary>
public sealed class CompanionGatewayOptions
{
    public const string SectionName = "CompanionGateway";

    /// <summary>TCP port the gateway listens on (must match the QR pairing URL port).</summary>
    public int Port { get; set; } = 49322;

    /// <summary>Directory of the built SPA to serve. When null/missing, only the API is served.</summary>
    public string? StaticPath { get; set; }
}
