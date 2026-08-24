using System.ComponentModel.DataAnnotations;

namespace PetHub.API.DTOs.User;

public class ForgotPasswordDto
{
    [Required]
    [EmailAddress]
    [StringLength(100)]
    public string Email { get; set; } = string.Empty;
}
