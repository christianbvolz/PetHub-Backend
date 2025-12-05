namespace PetHub.API.DTOs.User;

public class RevokeRequestDto
{
    // Optional: if not provided, controller will attempt to read the cookie `refreshToken`.
    public string? RefreshToken { get; set; }
}
