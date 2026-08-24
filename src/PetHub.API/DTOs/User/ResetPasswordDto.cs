using System.ComponentModel.DataAnnotations;

namespace PetHub.API.DTOs.User;

public class ResetPasswordDto
{
    [Required]
    public string Token { get; set; } = string.Empty;

    [Required]
    [StringLength(20, MinimumLength = 6)]
    public string NewPassword { get; set; } = string.Empty;
}
