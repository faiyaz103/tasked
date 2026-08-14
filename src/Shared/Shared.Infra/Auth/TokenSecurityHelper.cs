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
}