using Microsoft.AspNetCore.SignalR;
using pethub.Models;

namespace pethub.Hubs;

public class ChatHub : Hub
{
    // Método chamado quando alguém entra na tela de chat
    public async Task JoinChat(string conversationId)
    {
        // Adiciona o usuário (ConnectionId) a um grupo específico
        await Groups.AddToGroupAsync(Context.ConnectionId, conversationId);
    }

    // Método chamado quando alguém envia uma mensagem
    public async Task SendMessage(string conversationId, string senderName, string message)
    {
        // Envia APENAS para quem está nesse grupo (Sala)
        await Clients.Group(conversationId).SendAsync("ReceiveMessage", senderName, message);
    }
}
