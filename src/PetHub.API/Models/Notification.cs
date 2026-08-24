using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using PetHub.API.Enums;

namespace PetHub.API.Models;

public class Notification
{
    public int Id { get; set; }

    public Guid UserId { get; set; }

    [JsonIgnore]
    public User? User { get; set; }

    public NotificationType Type { get; set; }

    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [MaxLength(500)]
    public string Message { get; set; } = string.Empty;

    public int? AdoptionRequestId { get; set; }

    public int? PetId { get; set; }

    [MaxLength(50)]
    public string? PetName { get; set; }

    public bool IsRead { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
