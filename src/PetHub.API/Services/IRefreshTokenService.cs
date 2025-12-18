using PetHub.API.Models;

namespace PetHub.API.Services;

public interface IRefreshTokenService
{
    /// <summary>
    /// Creates a new refresh token for a user.
    /// </summary>
    /// <param name="userId">The user's ID.</param>
    /// <returns>The plain text refresh token (not stored).</returns>
    /// <summary>
    /// Creates a new refresh token for a user and returns the plain token.
    /// </summary>
    Task<string> CreateAsync(Guid userId);

    /// <summary>
    /// Rotates a refresh token, revoking the old one and creating a new one.
    /// </summary>
    /// <param name="tokenPlain">The plain text refresh token to rotate.</param>
    /// <returns>The new plain text refresh token.</returns>
    /// <summary>
    /// Rotates a refresh token, revoking the old one and creating a new one. Returns the new plain token.
    /// </summary>
    Task<string> RotateAsync(string tokenPlain);

    /// <summary>
    /// Revokes a refresh token.
    /// </summary>
    /// <param name="tokenPlain">The plain text refresh token to revoke.</param>
    /// <param name="reason">The reason for revocation.</param>
    /// <returns>True if revoked successfully.</returns>
    Task<bool> RevokeAsync(string tokenPlain, string reason = "Revoked by user");

    /// <summary>
    /// Gets a refresh token entity by its plain text value.
    /// </summary>
    /// <param name="tokenPlain">The plain text token.</param>
    /// <returns>The RefreshToken entity or null if not found.</returns>
    Task<RefreshToken?> GetByTokenAsync(string tokenPlain);
}
