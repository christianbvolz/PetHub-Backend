using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PetHub.API.Common;
using PetHub.API.DTOs.Common;
using PetHub.API.DTOs.Notification;
using PetHub.API.Services;

namespace PetHub.API.Controllers;

[ApiController]
[Route("api/notifications")]
[Authorize]
public class NotificationsController(INotificationService notificationService) : ApiControllerBase
{
    /// <summary>
    /// Lists in-app notifications for the authenticated user, newest first
    /// </summary>
    /// <returns>Notifications for the current user</returns>
    /// <response code="200">Notifications retrieved successfully</response>
    /// <response code="401">User not authenticated or invalid token</response>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<NotificationResponseDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult> GetNotifications()
    {
        var userIdResult = GetUserIdOrUnauthorized();
        if (userIdResult.Result is UnauthorizedObjectResult unauthorized)
            return unauthorized;

        var notifications = await notificationService.GetForUserAsync(userIdResult.Value);
        return Success(notifications);
    }

    /// <summary>
    /// Returns how many notifications the authenticated user has not read
    /// </summary>
    /// <returns>Unread notification count</returns>
    /// <response code="200">Count retrieved successfully</response>
    /// <response code="401">User not authenticated or invalid token</response>
    [HttpGet("unread-count")]
    [ProducesResponseType(typeof(ApiResponse<UnreadCountDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult> GetUnreadCount()
    {
        var userIdResult = GetUserIdOrUnauthorized();
        if (userIdResult.Result is UnauthorizedObjectResult unauthorized)
            return unauthorized;

        var count = await notificationService.GetUnreadCountAsync(userIdResult.Value);
        return Success(new UnreadCountDto { Count = count });
    }

    /// <summary>
    /// Marks a single notification as read
    /// </summary>
    /// <param name="id">Notification ID</param>
    /// <returns>The updated notification</returns>
    /// <response code="200">Notification marked as read</response>
    /// <response code="401">User not authenticated or invalid token</response>
    /// <response code="404">Notification not found</response>
    [HttpPost("{id:int}/read")]
    [ProducesResponseType(typeof(ApiResponse<NotificationResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult> MarkAsRead(int id)
    {
        var userIdResult = GetUserIdOrUnauthorized();
        if (userIdResult.Result is UnauthorizedObjectResult unauthorized)
            return unauthorized;

        var notification = await notificationService.MarkAsReadAsync(id, userIdResult.Value);
        if (notification == null)
            return NotFound("Notification not found");

        return Success(notification);
    }

    /// <summary>
    /// Marks every unread notification of the authenticated user as read
    /// </summary>
    /// <returns>How many notifications were marked as read</returns>
    /// <response code="200">Notifications marked as read</response>
    /// <response code="401">User not authenticated or invalid token</response>
    [HttpPost("read-all")]
    [ProducesResponseType(typeof(ApiResponse<MarkReadResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult> MarkAllAsRead()
    {
        var userIdResult = GetUserIdOrUnauthorized();
        if (userIdResult.Result is UnauthorizedObjectResult unauthorized)
            return unauthorized;

        var markedCount = await notificationService.MarkAllAsReadAsync(userIdResult.Value);
        return Success(new MarkReadResultDto { MarkedCount = markedCount });
    }
}
