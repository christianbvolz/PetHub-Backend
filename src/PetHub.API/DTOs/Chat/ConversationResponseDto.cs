using PetHub.API.DTOs.User;

namespace PetHub.API.DTOs.Chat;

public class ConversationResponseDto
{
    public required int Id { get; set; }
    public int? PetId { get; set; }
    public string? PetName { get; set; }
    public int? AdoptionRequestId { get; set; }
    public required PublicUserResponseDto OtherParticipant { get; set; }
    public required DateTime LastMessageAt { get; set; }
    public required int UnreadCount { get; set; }
    public ChatMessageResponseDto? LastMessage { get; set; }
}
