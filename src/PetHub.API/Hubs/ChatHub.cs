using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using PetHub.API.DTOs.Chat;
using PetHub.API.Services;

namespace PetHub.API.Hubs;

[Authorize]
public class ChatHub(IChatService chatService) : Hub
{
    public const string ReceiveMessageEvent = "ReceiveMessage";
    public const string MessagesReadEvent = "MessagesRead";

    public static string GroupName(int conversationId) => $"conversation-{conversationId}";

    public async Task JoinChat(int conversationId)
    {
        var userId = GetUserId();

        try
        {
            await chatService.GetConversationAsync(conversationId, userId);
        }
        catch (Exception ex) when (ex is KeyNotFoundException or UnauthorizedAccessException)
        {
            throw new HubException(ex.Message);
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, GroupName(conversationId));
    }

    public async Task SendMessage(int conversationId, string message)
    {
        var userId = GetUserId();

        ChatMessageResponseDto saved;
        try
        {
            saved = await chatService.SendMessageAsync(conversationId, userId, message);
        }
        catch (Exception ex)
            when (ex is KeyNotFoundException or UnauthorizedAccessException or ArgumentException)
        {
            throw new HubException(ex.Message);
        }

        await Clients.Group(GroupName(conversationId)).SendAsync(ReceiveMessageEvent, saved);
    }

    public async Task MarkAsRead(int conversationId)
    {
        var userId = GetUserId();

        MessagesReadDto result;
        try
        {
            result = await chatService.MarkAsReadAsync(conversationId, userId);
        }
        catch (Exception ex) when (ex is KeyNotFoundException or UnauthorizedAccessException)
        {
            throw new HubException(ex.Message);
        }

        await Clients.Group(GroupName(conversationId)).SendAsync(MessagesReadEvent, result);
    }

    private Guid GetUserId()
    {
        var value =
            Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? Context.User?.FindFirst("sub")?.Value;

        if (!Guid.TryParse(value, out var userId))
            throw new HubException("Unauthorized");

        return userId;
    }
}
