using System.ComponentModel.DataAnnotations;
using PetHub.API.Enums;

namespace PetHub.API.Models;

public class AuthToken
{
    public int Id { get; set; }

    [Required]
    public Guid UserId { get; set; }

    [Required]
    [MaxLength(64)]
    public string TokenHash { get; set; } = string.Empty;

    public AuthTokenPurpose Purpose { get; set; }

    public DateTime ExpiresAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UsedAt { get; set; }

    public User? User { get; set; }
}
