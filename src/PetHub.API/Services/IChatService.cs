using PetHub.API.DTOs.Chat;

namespace PetHub.API.Services;

public interface IChatService
{
    Task<(ConversationResponseDto Conversation, bool Created)> GetOrCreateForPetAsync(
        int petId,
        Guid currentUserId
    );

    Task<(ConversationResponseDto Conversation, bool Created)> GetOrCreateForAdoptionRequestAsync(
        int adoptionRequestId,
        Guid currentUserId
    );

    Task<List<ConversationResponseDto>> GetInboxAsync(Guid userId);

    Task<ConversationResponseDto> GetConversationAsync(int conversationId, Guid userId);

    Task<List<ChatMessageResponseDto>> GetMessagesAsync(
        int conversationId,
        Guid userId,
        int? beforeId,
        int pageSize
    );

    Task<ChatMessageResponseDto> SendMessageAsync(
        int conversationId,
        Guid userId,
        string content
    );

    Task<MessagesReadDto> MarkAsReadAsync(int conversationId, Guid userId);

    Task<bool> IsParticipantAsync(int conversationId, Guid userId);
}
