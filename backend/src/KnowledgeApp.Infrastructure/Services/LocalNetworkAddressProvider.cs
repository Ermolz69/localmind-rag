using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using KnowledgeApp.Application.Abstractions;

namespace KnowledgeApp.Infrastructure.Services;

/// <summary>
/// Resolves the machine's primary private IPv4 address by inspecting active,
/// non-loopback network interfaces. Read-only: it does not open any socket or
/// listener.
/// </summary>
public sealed class LocalNetworkAddressProvider : ILocalNetworkAddressProvider
{
    public string? GetLocalNetworkAddress()
    {
        try
        {
            IEnumerable<NetworkInterface> interfaces = NetworkInterface
                .GetAllNetworkInterfaces()
                .Where(nic =>
                    nic.OperationalStatus == OperationalStatus.Up
                    && nic.NetworkInterfaceType != NetworkInterfaceType.Loopback
                    && nic.NetworkInterfaceType != NetworkInterfaceType.Tunnel);

            foreach (NetworkInterface nic in interfaces)
            {
                foreach (UnicastIPAddressInformation address in nic.GetIPProperties().UnicastAddresses)
                {
                    if (address.Address.AddressFamily == AddressFamily.InterNetwork
                        && IsPrivate(address.Address))
                    {
                        return address.Address.ToString();
                    }
                }
            }
        }
        catch (NetworkInformationException)
        {
            return null;
        }

        return null;
    }

    private static bool IsPrivate(IPAddress address)
    {
        byte[] bytes = address.GetAddressBytes();

        // 10.0.0.0/8, 172.16.0.0/12, 192.168.0.0/16
        return bytes[0] switch
        {
            10 => true,
            172 => bytes[1] >= 16 && bytes[1] <= 31,
            192 => bytes[1] == 168,
            _ => false,
        };
    }
}
