using System.Security.Cryptography;
using System.Text;

namespace PetHub.API.Utils;

public static class RefreshTokenHelper
{
    /// <summary>
    /// Generates a cryptographically secure random token.
    /// </summary>
    /// <param name="length">The desired length of the token in bytes.</param>
    /// <returns>A URL-safe, base64-encoded random string.</returns>
    public static string GenerateSecureToken(int length = 32)
    {
        var randomNumber = new byte[length];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        // Use URL-safe Base64 encoding (replace + with -, / with _, and remove padding =)
        return Convert
            .ToBase64String(randomNumber)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
    }

    /// <summary>
    /// Computes the SHA256 hash of a string.
    /// </summary>
    /// <param name="input">The string to hash.</param>
    /// <returns>The SHA256 hash as a hex string.</returns>
    public static string ComputeSha256Hash(string input)
    {
        using var sha256 = SHA256.Create();
        var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
        var builder = new StringBuilder(64);
        foreach (var b in bytes)
        {
            builder.Append(b.ToString("x2"));
        }
        return builder.ToString();
    }
}
