namespace PetHub.API.DTOs.Chat;

public class ChatMessageResponseDto
{
    public required int Id { get; set; }
    public required int ConversationId { get; set; }
    public required Guid SenderId { get; set; }
    public required string SenderName { get; set; }
    public required string Content { get; set; }
    public required DateTime SentAt { get; set; }
    public required bool IsRead { get; set; }
}
