using PetHub.API.Enums;

namespace PetHub.API.DTOs.Notification;

public class NotificationResponseDto
{
    public required int Id { get; set; }
    public required NotificationType Type { get; set; }
    public required string Title { get; set; }
    public required string Message { get; set; }
    public int? AdoptionRequestId { get; set; }
    public int? PetId { get; set; }
    public string? PetName { get; set; }
    public required bool IsRead { get; set; }
    public required DateTime CreatedAt { get; set; }
}
