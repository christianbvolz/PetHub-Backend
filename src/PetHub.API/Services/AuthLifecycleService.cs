using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PetHub.API.Configuration;
using PetHub.API.Data;
using PetHub.API.Enums;
using PetHub.API.Models;
using PetHub.API.Utils;

namespace PetHub.API.Services;

public class AuthLifecycleService(
    AppDbContext dbContext,
    IEmailSender emailSender,
    IRefreshTokenService refreshTokenService,
    IOptions<AuthLifecycleSettings> settings,
    TimeProvider timeProvider,
    ILogger<AuthLifecycleService> logger
) : IAuthLifecycleService
{
    public const string InvalidTokenMessage = "Invalid or expired token.";
    public const string GenericVerificationMessage =
        "If an account exists for that email and it is not yet verified, a new verification link has been sent.";
    public const string GenericPasswordResetMessage =
        "If an account exists for that email, a reset link has been sent.";
    public const string EmailVerifiedMessage = "Email verified successfully.";
    public const string PasswordResetSuccessMessage =
        "Password reset successfully. Please login with your new password.";

    private readonly AuthLifecycleSettings _settings = settings.Value;

    public async Task SendVerificationEmailAsync(
        User user,
        CancellationToken cancellationToken = default
    )
    {
        if (user.EmailVerified)
            return;

        var token = await IssueTokenAsync(
            user.Id,
            AuthTokenPurpose.EmailVerification,
            TimeSpan.FromHours(_settings.EmailVerificationExpiresHours),
            cancellationToken
        );

        var url = BuildFrontendUrl(_settings.VerifyEmailPath, token);
        var (text, html) = AuthEmailTemplates.Verification(
            user.Name,
            url,
            token,
            _settings.EmailVerificationExpiresHours
        );

        await TrySendAsync(
            user.Email,
            AuthEmailTemplates.VerificationSubject,
            text,
            html,
            cancellationToken
        );
    }

    public async Task RequestVerificationEmailAsync(
        string email,
        CancellationToken cancellationToken = default
    )
    {
        var user = await FindByEmailAsync(email, cancellationToken);
        if (user == null || user.EmailVerified)
            return;

        await SendVerificationEmailAsync(user, cancellationToken);
    }

    public async Task VerifyEmailAsync(string token, CancellationToken cancellationToken = default)
    {
        var authToken = await ConsumeTokenAsync(
            token,
            AuthTokenPurpose.EmailVerification,
            cancellationToken
        );

        var user =
            authToken.User
            ?? await dbContext.Users.FirstOrDefaultAsync(
                u => u.Id == authToken.UserId,
                cancellationToken
            );

        if (user == null)
            throw new InvalidOperationException(InvalidTokenMessage);

        user.EmailVerified = true;
        user.EmailVerifiedAt = timeProvider.GetUtcNow().UtcDateTime;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task RequestPasswordResetAsync(
        string email,
        CancellationToken cancellationToken = default
    )
    {
        var user = await FindByEmailAsync(email, cancellationToken);
        if (user == null)
            return;

        var token = await IssueTokenAsync(
            user.Id,
            AuthTokenPurpose.PasswordReset,
            TimeSpan.FromHours(_settings.PasswordResetExpiresHours),
            cancellationToken
        );

        var url = BuildFrontendUrl(_settings.ResetPasswordPath, token);
        var (text, html) = AuthEmailTemplates.PasswordReset(
            user.Name,
            url,
            token,
            _settings.PasswordResetExpiresHours
        );

        await TrySendAsync(
            user.Email,
            AuthEmailTemplates.PasswordResetSubject,
            text,
            html,
            cancellationToken
        );
    }

    public async Task ResetPasswordAsync(
        string token,
        string newPassword,
        CancellationToken cancellationToken = default
    )
    {
        var authToken = await ConsumeTokenAsync(
            token,
            AuthTokenPurpose.PasswordReset,
            cancellationToken
        );

        var user =
            authToken.User
            ?? await dbContext.Users.FirstOrDefaultAsync(
                u => u.Id == authToken.UserId,
                cancellationToken
            );

        if (user == null)
            throw new InvalidOperationException(InvalidTokenMessage);

        user.PasswordHash = PasswordHelper.HashPassword(newPassword);
        await dbContext.SaveChangesAsync(cancellationToken);

        await refreshTokenService.RevokeAllForUserAsync(
            user.Id,
            "Password reset",
            cancellationToken
        );
    }

    private async Task<User?> FindByEmailAsync(string email, CancellationToken cancellationToken)
    {
        return await dbContext.Users.FirstOrDefaultAsync(
            u => u.Email == email,
            cancellationToken
        );
    }

    private async Task<string> IssueTokenAsync(
        Guid userId,
        AuthTokenPurpose purpose,
        TimeSpan lifetime,
        CancellationToken cancellationToken
    )
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;

        var previous = await dbContext
            .AuthTokens.Where(t =>
                t.UserId == userId && t.Purpose == purpose && t.UsedAt == null
            )
            .ToListAsync(cancellationToken);

        foreach (var existing in previous)
            existing.UsedAt = now;

        var plainToken = RefreshTokenHelper.GenerateSecureToken();
        dbContext.AuthTokens.Add(
            new AuthToken
            {
                UserId = userId,
                TokenHash = RefreshTokenHelper.ComputeSha256Hash(plainToken),
                Purpose = purpose,
                CreatedAt = now,
                ExpiresAt = now.Add(lifetime),
            }
        );

        await dbContext.SaveChangesAsync(cancellationToken);
        return plainToken;
    }

    private async Task<AuthToken> ConsumeTokenAsync(
        string token,
        AuthTokenPurpose purpose,
        CancellationToken cancellationToken
    )
    {
        if (string.IsNullOrWhiteSpace(token))
            throw new InvalidOperationException(InvalidTokenMessage);

        var hash = RefreshTokenHelper.ComputeSha256Hash(token);
        var now = timeProvider.GetUtcNow().UtcDateTime;

        var authToken = await dbContext
            .AuthTokens.Include(t => t.User)
            .FirstOrDefaultAsync(
                t => t.TokenHash == hash && t.Purpose == purpose,
                cancellationToken
            );

        if (authToken == null || authToken.UsedAt != null || authToken.ExpiresAt <= now)
            throw new InvalidOperationException(InvalidTokenMessage);

        authToken.UsedAt = now;
        await dbContext.SaveChangesAsync(cancellationToken);
        return authToken;
    }

    private string BuildFrontendUrl(string path, string token)
    {
        var baseUrl = _settings.FrontendBaseUrl.TrimEnd('/');
        var normalizedPath = path.StartsWith('/') ? path : $"/{path}";
        return $"{baseUrl}{normalizedPath}?token={Uri.EscapeDataString(token)}";
    }

    private async Task TrySendAsync(
        string to,
        string subject,
        string textBody,
        string htmlBody,
        CancellationToken cancellationToken
    )
    {
        try
        {
            await emailSender.SendAsync(to, subject, textBody, htmlBody, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send '{Subject}' email to {To}", subject, to);
        }
    }
}
