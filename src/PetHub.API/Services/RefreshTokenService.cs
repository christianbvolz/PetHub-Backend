using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PetHub.API.Configuration;
using PetHub.API.Data;
using PetHub.API.Models;
using PetHub.API.Utils;

namespace PetHub.API.Services;

public class RefreshTokenService(AppDbContext dbContext, IOptions<RefreshTokenSettings> settings)
    : IRefreshTokenService
{
    private readonly AppDbContext _db = dbContext;
    private readonly RefreshTokenSettings _settings = settings.Value;

    public async Task<string> CreateAsync(Guid userId)
    {
        var plainToken = RefreshTokenHelper.GenerateSecureToken();
        var hash = RefreshTokenHelper.ComputeSha256Hash(plainToken);
        var expiresAt = DateTime.UtcNow.AddDays(_settings.ExpiresAtDays);
        var entity = new RefreshToken
        {
            UserId = userId,
            TokenHash = hash,
            ExpiresAt = expiresAt,
            CreatedAt = DateTime.UtcNow,
        };

        _db.RefreshTokens.Add(entity);
        await _db.SaveChangesAsync();

        return plainToken;
    }

    public async Task<RefreshToken?> GetByTokenAsync(string tokenPlain)
    {
        var hash = RefreshTokenHelper.ComputeSha256Hash(tokenPlain);
        return await _db
            .RefreshTokens.Include(rt => rt.User)
            .FirstOrDefaultAsync(rt => rt.TokenHash == hash);
    }

    public async Task<string> RotateAsync(string tokenPlain)
    {
        var hash = RefreshTokenHelper.ComputeSha256Hash(tokenPlain);
        var existingToken = await _db
            .RefreshTokens.Include(rt => rt.User)
            .FirstOrDefaultAsync(rt => rt.TokenHash == hash);

        if (existingToken == null)
        {
            throw new InvalidOperationException("Refresh token not found.");
        }

        if (existingToken.RevokedAt != null || existingToken.ExpiresAt <= DateTime.UtcNow)
        {
            // Token is invalid, potentially compromised. Revoke all active tokens for this user.
            var now = DateTime.UtcNow;

            await _db
                .RefreshTokens.Where(t => t.UserId == existingToken.UserId && t.RevokedAt == null)
                .ExecuteUpdateAsync(s =>
                    s.SetProperty(t => t.RevokedAt, _ => now)
                        .SetProperty(
                            t => t.ReasonRevoked,
                            _ => "Attempted reuse of revoked or expired token"
                        )
                );

            throw new InvalidOperationException(
                "Refresh token has been invalidated. All sessions have been logged out."
            );
        }

        // Create new token
        var newPlainToken = RefreshTokenHelper.GenerateSecureToken();
        var newHash = RefreshTokenHelper.ComputeSha256Hash(newPlainToken);
        var expiresAt = DateTime.UtcNow.AddDays(_settings.ExpiresAtDays);
        var newEntity = new RefreshToken
        {
            UserId = existingToken.UserId,
            TokenHash = newHash,
            ExpiresAt = expiresAt,
            CreatedAt = DateTime.UtcNow,
        };
        _db.RefreshTokens.Add(newEntity);

        // Revoke old token
        existingToken.RevokedAt = DateTime.UtcNow;
        existingToken.ReplacedByTokenHash = newHash;
        existingToken.ReasonRevoked = "Rotated";

        await _db.SaveChangesAsync();

        return newPlainToken;
    }

    public async Task<bool> RevokeAsync(string tokenPlain, string reason = "Revoked by user")
    {
        if (string.IsNullOrEmpty(tokenPlain))
        {
            return false;
        }

        var hash = RefreshTokenHelper.ComputeSha256Hash(tokenPlain);
        var existing = await _db.RefreshTokens.FirstOrDefaultAsync(rt => rt.TokenHash == hash);

        if (existing == null || existing.RevokedAt != null)
        {
            return false; // Token doesn't exist or already revoked
        }

        existing.RevokedAt = DateTime.UtcNow;
        existing.ReasonRevoked = reason;

        await _db.SaveChangesAsync();
        return true;
    }
}
