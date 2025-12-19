namespace PetHub.API.DTOs.User;

/// <summary>
/// DTO for refreshing an access token using a refresh token.
/// </summary>
/// <remarks>
/// ⚠️ Security Note: In production environments, refresh tokens should ONLY be sent via HttpOnly cookies
/// to prevent XSS attacks. This DTO accepts tokens in the request body primarily to facilitate
/// integration and unit testing. For production use, leave this property null and rely on the
/// 'refreshToken' cookie instead.
/// </remarks>
public class RefreshRequestDto
{
    /// <summary>
    /// Optional refresh token. If not provided in the request body, the controller will read it from the HttpOnly cookie named 'refreshToken'.
    /// </summary>
    public string? RefreshToken { get; set; }
}
