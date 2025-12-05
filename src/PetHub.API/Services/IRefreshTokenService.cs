using PetHub.API.Models;

namespace PetHub.API.Services;

public interface IRefreshTokenService
{
    /// <summary>
    /// Creates a new refresh token for a user.
    /// </summary>
    /// <param name="userId">The user's ID.</param>
    /// <param name="ipAddress">The user's IP address.</param>
    /// <returns>The plain text refresh token (not stored).</returns>
    Task<string> CreateAsync(Guid userId, string? ipAddress);

    /// <summary>
    /// Rotates a refresh token, revoking the old one and creating a new one.
    /// </summary>
    /// <param name="tokenPlain">The plain text refresh token to rotate.</param>
    /// <param name="ipAddress">The user's IP address.</param>
    /// <returns>The new plain text refresh token.</returns>
    Task<string> RotateAsync(string tokenPlain, string? ipAddress);

    /// <summary>
    /// Revokes a refresh token.
    /// </summary>
    /// <param name="tokenPlain">The plain text refresh token to revoke.</param>
    /// <param name="ipAddress">The user's IP address.</param>
    /// <param name="reason">The reason for revocation.</param>
    /// <returns>True if revoked successfully.</returns>
    Task<bool> RevokeAsync(string tokenPlain, string? ipAddress, string reason = "Revoked by user");

    /// <summary>
    /// Gets a refresh token entity by its plain text value.
    /// </summary>
    /// <param name="tokenPlain">The plain text token.</param>
    /// <returns>The RefreshToken entity or null if not found.</returns>
    Task<RefreshToken?> GetByTokenAsync(string tokenPlain);
}
