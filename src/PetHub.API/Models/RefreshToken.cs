using System.ComponentModel.DataAnnotations;

namespace PetHub.API.Models;

public class RefreshToken
{
    public int Id { get; set; }

    [Required]
    public string TokenHash { get; set; } = string.Empty; // SHA256 hash

    [Required]
    public Guid UserId { get; set; }

    public DateTime ExpiresAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public string? CreatedByIp { get; set; }

    public DateTime? RevokedAt { get; set; }

    public string? RevokedByIp { get; set; }

    public string? ReplacedByTokenHash { get; set; }

    public string? ReasonRevoked { get; set; }

    // Navigation
    public User? User { get; set; }
}
