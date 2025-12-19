namespace PetHub.API.DTOs.User;

/// <summary>
/// DTO for revoking a refresh token (logout).
/// </summary>
/// <remarks>
/// ⚠️ Security Note: In production environments, refresh tokens should ONLY be sent via HttpOnly cookies
/// to prevent XSS attacks. This DTO accepts tokens in the request body primarily to facilitate
/// integration and unit testing. For production use, leave this property null and rely on the
/// 'refreshToken' cookie instead.
/// </remarks>
public class RevokeRequestDto
{
    /// <summary>
    /// Optional refresh token. If not provided in the request body, the controller will read it from the HttpOnly cookie named 'refreshToken'.
    /// </summary>
    public string? RefreshToken { get; set; }
}
