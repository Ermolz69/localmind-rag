using System.Security.Cryptography;
using System.Text;
using KnowledgeApp.Application.Abstractions;

namespace KnowledgeApp.Infrastructure.Services;

public sealed class LocalDeviceIdentityProvider : ILocalDeviceIdentityProvider
{
    public LocalDeviceIdentity GetIdentity()
    {
        string machineName = Environment.MachineName;
        string userName = Environment.UserName;
        string rawKey = $"{machineName}|{userName}";
        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(rawKey));
        string deviceKey = Convert.ToHexString(hash);

        return new LocalDeviceIdentity(deviceKey, machineName);
    }
}
