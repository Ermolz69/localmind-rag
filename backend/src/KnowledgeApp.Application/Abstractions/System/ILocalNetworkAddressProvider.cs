namespace KnowledgeApp.Application.Abstractions;

/// <summary>Resolves the machine's local-network (LAN) address for Companion Mode.</summary>
public interface ILocalNetworkAddressProvider
{
    /// <summary>
    /// Returns the best private IPv4 address for this machine on the local network,
    /// or <c>null</c> when none can be determined.
    /// </summary>
    string? GetLocalNetworkAddress();
}
