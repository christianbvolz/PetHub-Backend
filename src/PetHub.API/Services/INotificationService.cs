using PetHub.API.DTOs.Notification;
using PetHub.API.Enums;
using PetHub.API.Models;

namespace PetHub.API.Services;

public interface INotificationService
{
    Task NotifyAdoptionEventAsync(
        NotificationType type,
        Guid recipientUserId,
        AdoptionRequest request
    );

    Task<List<NotificationResponseDto>> GetForUserAsync(Guid userId);

    Task<int> GetUnreadCountAsync(Guid userId);

    Task<NotificationResponseDto?> MarkAsReadAsync(int notificationId, Guid userId);

    Task<int> MarkAllAsReadAsync(Guid userId);
}
