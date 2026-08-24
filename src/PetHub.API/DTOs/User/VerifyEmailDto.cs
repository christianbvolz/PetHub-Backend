using System.ComponentModel.DataAnnotations;

namespace PetHub.API.DTOs.User;

public class VerifyEmailDto
{
    [Required]
    public string Token { get; set; } = string.Empty;
}
