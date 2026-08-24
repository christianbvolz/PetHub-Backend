using System.ComponentModel.DataAnnotations;

namespace PetHub.API.DTOs.User;

public class ResendVerificationDto
{
    [Required]
    [EmailAddress]
    [StringLength(100)]
    public string Email { get; set; } = string.Empty;
}
