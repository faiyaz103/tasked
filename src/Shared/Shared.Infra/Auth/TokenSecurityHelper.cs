using System.Security.Cryptography;
using System.Text;

namespace Shared.Infra.Auth;
public static class TokenSecurityHelper
{
    public static string DoubleHashToken(string rawToken)
    {
        using var sha256 = SHA256.Create();
        var sha256Bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(rawToken));
        string sha256Hex = Convert.ToHexString(sha256Bytes);

        return BCrypt.Net.BCrypt.HashPassword(sha256Hex, workFactor: 10);
    }

    public static bool VerifyDoubleHashedToken(string rawToken, string storedBCryptHash)
    {
        if (string.IsNullOrWhiteSpace(rawToken) || string.IsNullOrWhiteSpace(storedBCryptHash))
        {
            return false;
        }

        // 1. Convert incoming raw token to SHA-256 Hex string first
        using var sha256 = SHA256.Create();
        var sha256Bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(rawToken));
        string sha256Hex = Convert.ToHexString(sha256Bytes);

        // 2. Verify the SHA-256 hex string against the stored BCrypt hash
        return BCrypt.Net.BCrypt.Verify(sha256Hex, storedBCryptHash);
    }
}