using Microsoft.Extensions.Options;
using PetHub.API.Configuration;

namespace PetHub.API.Utils;

/// <summary>
/// Provider for CookieOptions so we can inject configuration via IOptions.
/// Uses IOptions for static configuration (ExpiresAtDays rarely changes).
/// </summary>
public class CookieOptionsProvider : ICookieOptionsProvider
{
    private readonly RefreshTokenSettings _settings;

    public CookieOptionsProvider(IOptions<RefreshTokenSettings> options)
    {
        _settings = options?.Value ?? throw new ArgumentNullException(nameof(options));
    }

    public CookieOptions CreateRefreshCookieOptions()
    {
        return new CookieOptions
        {
            HttpOnly = true,
            Expires = DateTime.UtcNow.AddDays(_settings.ExpiresAtDays),
            Secure = true,
            SameSite = SameSiteMode.Lax,
        };
    }

    public CookieOptions CreateDeleteCookieOptions()
    {
        return new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Lax,
            Expires = DateTime.UtcNow.AddDays(-1),
        };
    }
}
