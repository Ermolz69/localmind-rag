using System.Security.Cryptography;
using System.Text;

namespace KnowledgeApp.Application.Companion;

/// <summary>
/// Hashes device tokens for storage. Tokens are high-entropy random values, so a
/// plain SHA-256 (no salt) is sufficient and allows an indexed lookup by hash.
/// </summary>
public static class CompanionTokenHasher
{
    public static string Hash(string token)
    {
        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
