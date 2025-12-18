using Microsoft.AspNetCore.SignalR;

namespace PetHub.API.Hubs;

public class ChatHub : Hub
{
    // Method called when someone enters the chat screen
    public async Task JoinChat(string conversationId)
    {
        // Adds the user (ConnectionId) to a specific group
        await Groups.AddToGroupAsync(Context.ConnectionId, conversationId);
    }

    // Method called when someone sends a message
    public async Task SendMessage(string conversationId, string senderName, string message)
    {
        // Sends ONLY to those in this group (Room)
        await Clients.Group(conversationId).SendAsync("ReceiveMessage", senderName, message);
    }
}
