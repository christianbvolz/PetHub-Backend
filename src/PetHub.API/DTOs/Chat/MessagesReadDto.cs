namespace PetHub.API.DTOs.Chat;

public class MessagesReadDto
{
    public required int ConversationId { get; set; }
    public required Guid ReadByUserId { get; set; }
    public required int MarkedCount { get; set; }
}
