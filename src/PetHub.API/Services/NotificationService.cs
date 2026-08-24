using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using PetHub.API.Data;
using PetHub.API.DTOs.Notification;
using PetHub.API.Enums;
using PetHub.API.Hubs;
using PetHub.API.Mappings;
using PetHub.API.Models;

namespace PetHub.API.Services;

public class NotificationService(
    AppDbContext context,
    IHubContext<NotificationHub> hubContext,
    ILogger<NotificationService> logger
) : INotificationService
{
    public const string FallbackPetName = "this pet";
    public const string FallbackAdopterName = "Someone";

    public async Task NotifyAdoptionEventAsync(
        NotificationType type,
        Guid recipientUserId,
        AdoptionRequest request
    )
    {
        if (recipientUserId == Guid.Empty)
            return;

        try
        {
            var (petName, adopterName) = await ResolveNamesAsync(request);
            var (title, message) = BuildMessage(type, petName, adopterName);

            var notification = new Notification
            {
                UserId = recipientUserId,
                Type = type,
                Title = title,
                Message = message,
                AdoptionRequestId = request.Id,
                PetId = request.PetId,
                PetName = petName,
                IsRead = false,
                CreatedAt = DateTime.UtcNow,
            };

            context.Notifications.Add(notification);
            await context.SaveChangesAsync();

            await hubContext
                .Clients.Group(NotificationHub.GroupName(recipientUserId))
                .SendAsync(NotificationHub.ReceiveNotificationEvent, notification.ToResponseDto());
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Failed to notify user {UserId} of {NotificationType} for adoption request {RequestId}",
                recipientUserId,
                type,
                request.Id
            );
        }
    }

    public async Task<List<NotificationResponseDto>> GetForUserAsync(Guid userId)
    {
        var notifications = await context
            .Notifications.AsNoTracking()
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync();

        return notifications.Select(n => n.ToResponseDto()).ToList();
    }

    public Task<int> GetUnreadCountAsync(Guid userId)
    {
        return context.Notifications.CountAsync(n => n.UserId == userId && !n.IsRead);
    }

    public async Task<NotificationResponseDto?> MarkAsReadAsync(int notificationId, Guid userId)
    {
        var notification = await context.Notifications.FirstOrDefaultAsync(n =>
            n.Id == notificationId && n.UserId == userId
        );

        if (notification == null)
            return null;

        if (!notification.IsRead)
        {
            notification.IsRead = true;
            await context.SaveChangesAsync();
        }

        return notification.ToResponseDto();
    }

    public async Task<int> MarkAllAsReadAsync(Guid userId)
    {
        var unread = await context
            .Notifications.Where(n => n.UserId == userId && !n.IsRead)
            .ToListAsync();

        foreach (var notification in unread)
            notification.IsRead = true;

        if (unread.Count > 0)
            await context.SaveChangesAsync();

        return unread.Count;
    }

    internal static (string Title, string Message) BuildMessage(
        NotificationType type,
        string petName,
        string adopterName
    )
    {
        return type switch
        {
            NotificationType.AdoptionRequestCreated => (
                "New adoption request",
                $"{adopterName} requested to adopt {petName}."
            ),
            NotificationType.AdoptionRequestApproved => (
                "Adoption request approved",
                $"Your request to adopt {petName} was approved."
            ),
            NotificationType.AdoptionRequestRejected => (
                "Adoption request rejected",
                $"Your request to adopt {petName} was rejected."
            ),
            NotificationType.AdoptionRequestCancelled => (
                "Adoption request cancelled",
                $"{adopterName} cancelled the request to adopt {petName}."
            ),
            _ => ("Adoption update", $"There is an update about {petName}."),
        };
    }

    private async Task<(string PetName, string AdopterName)> ResolveNamesAsync(
        AdoptionRequest request
    )
    {
        var petName = request.Pet?.Name;
        var adopterName = request.Adopter?.Name;

        if (string.IsNullOrWhiteSpace(petName))
        {
            var pet = await context
                .Pets.AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == request.PetId);
            petName = pet?.Name;
        }

        if (string.IsNullOrWhiteSpace(adopterName))
        {
            var adopter = await context
                .Users.AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == request.AdopterId);
            adopterName = adopter?.Name;
        }

        return (
            string.IsNullOrWhiteSpace(petName) ? FallbackPetName : petName,
            string.IsNullOrWhiteSpace(adopterName) ? FallbackAdopterName : adopterName
        );
    }
}
