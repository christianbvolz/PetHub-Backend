using Microsoft.EntityFrameworkCore;
using PetHub.API.Data;
using PetHub.API.Models;
using PetHub.API.Utils;

namespace PetHub.API.Services;

public class RefreshTokenService(AppDbContext dbContext) : IRefreshTokenService
{
    private readonly AppDbContext _db = dbContext;

    public async Task<string> CreateAsync(Guid userId, string? ipAddress)
    {
        var plainToken = TokenHelper.GenerateSecureToken();
        var hash = TokenHelper.ComputeSha256Hash(plainToken);

        var entity = new RefreshToken
        {
            UserId = userId,
            TokenHash = hash,
            ExpiresAt = DateTime.UtcNow.AddDays(14), // Configurable
            CreatedAt = DateTime.UtcNow,
            CreatedByIp = ipAddress,
        };

        _db.RefreshTokens.Add(entity);
        await _db.SaveChangesAsync();

        return plainToken;
    }

    public async Task<RefreshToken?> GetByTokenAsync(string tokenPlain)
    {
        var hash = TokenHelper.ComputeSha256Hash(tokenPlain);
        return await _db.RefreshTokens
            .Include(rt => rt.User)
            .FirstOrDefaultAsync(rt => rt.TokenHash == hash);
    }

    public async Task<string> RotateAsync(string tokenPlain, string? ipAddress)
    {
        var hash = TokenHelper.ComputeSha256Hash(tokenPlain);
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
            var activeTokens = await _db
                .RefreshTokens.Where(t => t.UserId == existingToken.UserId && t.RevokedAt == null)
                .ToListAsync();

            foreach (var token in activeTokens)
            {
                token.RevokedAt = DateTime.UtcNow;
                token.RevokedByIp = ipAddress;
                token.ReasonRevoked = "Attempted reuse of revoked or expired token";
            }
            await _db.SaveChangesAsync();
            throw new InvalidOperationException(
                "Refresh token has been invalidated. All sessions have been logged out."
            );
        }

        // Create new token
        var newPlainToken = TokenHelper.GenerateSecureToken();
        var newHash = TokenHelper.ComputeSha256Hash(newPlainToken);
        var newEntity = new RefreshToken
        {
            UserId = existingToken.UserId,
            TokenHash = newHash,
            ExpiresAt = DateTime.UtcNow.AddDays(14),
            CreatedAt = DateTime.UtcNow,
            CreatedByIp = ipAddress,
        };
        _db.RefreshTokens.Add(newEntity);

        // Revoke old token
        existingToken.RevokedAt = DateTime.UtcNow;
        existingToken.RevokedByIp = ipAddress;
        existingToken.ReplacedByTokenHash = newHash;
        existingToken.ReasonRevoked = "Rotated";

        await _db.SaveChangesAsync();

        return newPlainToken;
    }

    public async Task<bool> RevokeAsync(
        string tokenPlain,
        string? ipAddress,
        string reason = "Revoked by user"
    )
    {
        if (string.IsNullOrEmpty(tokenPlain))
        {
            return false;
        }

        var hash = TokenHelper.ComputeSha256Hash(tokenPlain);
        var existing = await _db.RefreshTokens.FirstOrDefaultAsync(rt => rt.TokenHash == hash);

        if (existing == null || existing.RevokedAt != null)
        {
            return false; // Token doesn't exist or already revoked
        }

        existing.RevokedAt = DateTime.UtcNow;
        existing.RevokedByIp = ipAddress;
        existing.ReasonRevoked = reason;

        await _db.SaveChangesAsync();
        return true;
    }
}
