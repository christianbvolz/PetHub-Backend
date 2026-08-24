using PetHub.API.DTOs.Notification;
using PetHub.API.Models;

namespace PetHub.API.Mappings;

public static class NotificationMappingExtensions
{
    public static NotificationResponseDto ToResponseDto(this Notification notification)
    {
        return new NotificationResponseDto
        {
            Id = notification.Id,
            Type = notification.Type,
            Title = notification.Title,
            Message = notification.Message,
            AdoptionRequestId = notification.AdoptionRequestId,
            PetId = notification.PetId,
            PetName = notification.PetName,
            IsRead = notification.IsRead,
            CreatedAt = notification.CreatedAt,
        };
    }
}
