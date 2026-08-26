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
    private readonly IHostEnvironment _environment;

    public CookieOptionsProvider(IOptions<RefreshTokenSettings> options, IHostEnvironment environment)
    {
        _settings = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _environment = environment ?? throw new ArgumentNullException(nameof(environment));
    }

    public CookieOptions CreateRefreshCookieOptions()
    {
        return new CookieOptions
        {
            HttpOnly = true,
            Expires = DateTime.UtcNow.AddDays(_settings.ExpiresAtDays),
            // Browsers reject Secure cookies on http://. Local HTTP avoids the
            // untrusted ASP.NET developer certificate (ERR_CERT_AUTHORITY_INVALID).
            Secure = !_environment.IsDevelopment(),
            SameSite = SameSiteMode.Lax,
        };
    }

    public CookieOptions CreateDeleteCookieOptions()
    {
        return new CookieOptions
        {
            HttpOnly = true,
            Secure = !_environment.IsDevelopment(),
            SameSite = SameSiteMode.Lax,
            Expires = DateTime.UtcNow.AddDays(-1),
        };
    }
}
