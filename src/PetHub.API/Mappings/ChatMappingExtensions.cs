using PetHub.API.DTOs.Chat;
using PetHub.API.Models;

namespace PetHub.API.Mappings;

public static class ChatMappingExtensions
{
    public static ChatMessageResponseDto ToResponseDto(this ChatMessage message)
    {
        return new ChatMessageResponseDto
        {
            Id = message.Id,
            ConversationId = message.ConversationId,
            SenderId = message.SenderId,
            SenderName = message.Sender?.Name ?? string.Empty,
            Content = message.Content,
            SentAt = message.SentAt,
            IsRead = message.IsRead,
        };
    }

    public static ConversationResponseDto ToResponseDto(
        this Conversation conversation,
        Guid currentUserId,
        ChatMessage? lastMessage = null,
        int unreadCount = 0
    )
    {
        var otherParticipant =
            conversation.UserAId == currentUserId ? conversation.UserB : conversation.UserA;

        return new ConversationResponseDto
        {
            Id = conversation.Id,
            PetId = conversation.PetId,
            PetName = conversation.Pet?.Name,
            AdoptionRequestId = conversation.AdoptionRequestId,
            OtherParticipant =
                otherParticipant?.ToPublicResponseDto()
                ?? throw new InvalidOperationException(
                    "Conversation participant is missing from the query."
                ),
            LastMessageAt = conversation.LastMessageAt,
            UnreadCount = unreadCount,
            LastMessage = lastMessage?.ToResponseDto(),
        };
    }
}
