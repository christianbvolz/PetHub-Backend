namespace PetHub.API.DTOs.User;

public class RefreshRequestDto
{
    // Optional: if not provided in the request body, the controller will read the cookie named `refreshToken`.
    public string? RefreshToken { get; set; }
}
