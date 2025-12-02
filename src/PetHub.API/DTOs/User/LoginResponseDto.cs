namespace PetHub.API.DTOs.User;

public class LoginResponseDto
{
    public required string Token { get; set; }
    public required UserResponseDto User { get; set; }
}
