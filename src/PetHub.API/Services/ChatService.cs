using Microsoft.EntityFrameworkCore;
using PetHub.API.Data;
using PetHub.API.DTOs.Chat;
using PetHub.API.Mappings;
using PetHub.API.Models;

namespace PetHub.API.Services;

public class ChatService(AppDbContext context) : IChatService
{
    public const int DefaultMessagePageSize = 50;
    public const int MaxMessagePageSize = 100;
    public const int MaxMessageContentLength = 2000;

    public async Task<(ConversationResponseDto Conversation, bool Created)> GetOrCreateForPetAsync(
        int petId,
        Guid currentUserId
    )
    {
        var pet = await context.Pets.AsNoTracking().FirstOrDefaultAsync(p => p.Id == petId);
        if (pet == null)
            throw new KeyNotFoundException("Pet not found");

        if (pet.UserId == currentUserId)
            throw new ArgumentException("You cannot start a conversation about your own pet");

        return await GetOrCreatePairAsync(
            petId,
            currentUserId,
            pet.UserId,
            currentUserId,
            adoptionRequestId: null,
            initialMessage: null,
            initialSenderId: null
        );
    }

    public async Task<(
        ConversationResponseDto Conversation,
        bool Created
    )> GetOrCreateForAdoptionRequestAsync(int adoptionRequestId, Guid currentUserId)
    {
        var request = await context
            .AdoptionRequests.Include(ar => ar.Pet)
            .FirstOrDefaultAsync(ar => ar.Id == adoptionRequestId);

        if (request == null)
            throw new KeyNotFoundException("Adoption request not found");

        if (request.Pet == null)
            throw new KeyNotFoundException("Pet not found");

        var ownerId = request.Pet.UserId;
        if (currentUserId != request.AdopterId && currentUserId != ownerId)
        {
            throw new UnauthorizedAccessException(
                "You don't have permission to open this conversation"
            );
        }

        return await GetOrCreatePairAsync(
            request.PetId,
            request.AdopterId,
            ownerId,
            currentUserId,
            adoptionRequestId,
            initialMessage: request.Message,
            initialSenderId: request.AdopterId
        );
    }

    public async Task<List<ConversationResponseDto>> GetInboxAsync(Guid userId)
    {
        var conversations = await ConversationsWithParticipants()
            .Where(c => c.UserAId == userId || c.UserBId == userId)
            .OrderByDescending(c => c.LastMessageAt)
            .ToListAsync();

        if (conversations.Count == 0)
            return [];

        var conversationIds = conversations.Select(c => c.Id).ToList();
        var lastMessages = await LoadLastMessagesAsync(conversationIds);
        var unreadCounts = await LoadUnreadCountsAsync(conversationIds, userId);

        return conversations
            .Select(conversation =>
                conversation.ToResponseDto(
                    userId,
                    lastMessages.GetValueOrDefault(conversation.Id),
                    unreadCounts.GetValueOrDefault(conversation.Id)
                )
            )
            .ToList();
    }

    public async Task<ConversationResponseDto> GetConversationAsync(
        int conversationId,
        Guid userId
    )
    {
        var conversation = await RequireParticipantAsync(conversationId, userId);
        var lastMessages = await LoadLastMessagesAsync([conversation.Id]);
        var unreadCounts = await LoadUnreadCountsAsync([conversation.Id], userId);

        return conversation.ToResponseDto(
            userId,
            lastMessages.GetValueOrDefault(conversation.Id),
            unreadCounts.GetValueOrDefault(conversation.Id)
        );
    }

    public async Task<List<ChatMessageResponseDto>> GetMessagesAsync(
        int conversationId,
        Guid userId,
        int? beforeId,
        int pageSize
    )
    {
        await RequireParticipantAsync(conversationId, userId);

        pageSize = Math.Clamp(pageSize, 1, MaxMessagePageSize);

        var query = context
            .ChatMessages.Include(m => m.Sender)
            .Where(m => m.ConversationId == conversationId);

        if (beforeId.HasValue)
        {
            var cursor = await context.ChatMessages.FirstOrDefaultAsync(m =>
                m.Id == beforeId.Value && m.ConversationId == conversationId
            );

            if (cursor != null)
            {
                query = query.Where(m =>
                    m.SentAt < cursor.SentAt || (m.SentAt == cursor.SentAt && m.Id < cursor.Id)
                );
            }
        }

        var messages = await query
            .OrderByDescending(m => m.SentAt)
            .ThenByDescending(m => m.Id)
            .Take(pageSize)
            .ToListAsync();

        return messages
            .OrderBy(m => m.SentAt)
            .ThenBy(m => m.Id)
            .Select(m => m.ToResponseDto())
            .ToList();
    }

    public async Task<ChatMessageResponseDto> SendMessageAsync(
        int conversationId,
        Guid userId,
        string content
    )
    {
        var conversation = await RequireParticipantAsync(conversationId, userId);
        var normalized = NormalizeContent(content);

        var message = new ChatMessage
        {
            ConversationId = conversation.Id,
            SenderId = userId,
            Content = normalized,
            SentAt = DateTime.UtcNow,
            IsRead = false,
        };

        conversation.LastMessageAt = message.SentAt;
        context.ChatMessages.Add(message);
        await context.SaveChangesAsync();

        await context.Entry(message).Reference(m => m.Sender).LoadAsync();

        return message.ToResponseDto();
    }

    public async Task<MessagesReadDto> MarkAsReadAsync(int conversationId, Guid userId)
    {
        await RequireParticipantAsync(conversationId, userId);

        var unread = await context
            .ChatMessages.Where(m =>
                m.ConversationId == conversationId && m.SenderId != userId && !m.IsRead
            )
            .ToListAsync();

        foreach (var message in unread)
        {
            message.IsRead = true;
        }

        if (unread.Count > 0)
            await context.SaveChangesAsync();

        return new MessagesReadDto
        {
            ConversationId = conversationId,
            ReadByUserId = userId,
            MarkedCount = unread.Count,
        };
    }

    public async Task<bool> IsParticipantAsync(int conversationId, Guid userId)
    {
        return await context.Conversations.AnyAsync(c =>
            c.Id == conversationId && (c.UserAId == userId || c.UserBId == userId)
        );
    }

    private async Task<(
        ConversationResponseDto Conversation,
        bool Created
    )> GetOrCreatePairAsync(
        int petId,
        Guid user1,
        Guid user2,
        Guid currentUserId,
        int? adoptionRequestId,
        string? initialMessage,
        Guid? initialSenderId
    )
    {
        if (user1 == user2)
            throw new ArgumentException("A conversation requires two different participants");

        var (userAId, userBId) = OrderParticipants(user1, user2);

        var existing = await FindByPetAndParticipantsAsync(petId, userAId, userBId);
        if (existing != null)
        {
            if (
                adoptionRequestId.HasValue
                && existing.AdoptionRequestId != adoptionRequestId.Value
            )
            {
                existing.AdoptionRequestId = adoptionRequestId;
                await context.SaveChangesAsync();
            }

            return (await MapConversationAsync(existing.Id, currentUserId), false);
        }

        var conversation = new Conversation
        {
            PetId = petId,
            UserAId = userAId,
            UserBId = userBId,
            AdoptionRequestId = adoptionRequestId,
            LastMessageAt = DateTime.UtcNow,
        };

        context.Conversations.Add(conversation);

        try
        {
            await context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            context.ChangeTracker.Clear();
            var raced = await FindByPetAndParticipantsAsync(petId, userAId, userBId);
            if (raced == null)
                throw;

            if (
                adoptionRequestId.HasValue
                && raced.AdoptionRequestId != adoptionRequestId.Value
            )
            {
                raced.AdoptionRequestId = adoptionRequestId;
                await context.SaveChangesAsync();
            }

            return (await MapConversationAsync(raced.Id, currentUserId), false);
        }

        if (
            !string.IsNullOrWhiteSpace(initialMessage)
            && initialSenderId.HasValue
            && (initialSenderId == userAId || initialSenderId == userBId)
        )
        {
            var seed = new ChatMessage
            {
                ConversationId = conversation.Id,
                SenderId = initialSenderId.Value,
                Content = NormalizeContent(initialMessage),
                SentAt = DateTime.UtcNow,
                IsRead = false,
            };
            conversation.LastMessageAt = seed.SentAt;
            context.ChatMessages.Add(seed);
            await context.SaveChangesAsync();
        }

        return (await MapConversationAsync(conversation.Id, currentUserId), true);
    }

    private async Task<Conversation?> FindByPetAndParticipantsAsync(
        int petId,
        Guid userAId,
        Guid userBId
    )
    {
        return await context.Conversations.FirstOrDefaultAsync(c =>
            c.PetId == petId && c.UserAId == userAId && c.UserBId == userBId
        );
    }

    private async Task<Conversation> RequireParticipantAsync(int conversationId, Guid userId)
    {
        var conversation = await ConversationsWithParticipants()
            .FirstOrDefaultAsync(c => c.Id == conversationId);

        if (conversation == null)
            throw new KeyNotFoundException("Conversation not found");

        if (conversation.UserAId != userId && conversation.UserBId != userId)
        {
            throw new UnauthorizedAccessException(
                "You don't have permission to access this conversation"
            );
        }

        return conversation;
    }

    private async Task<ConversationResponseDto> MapConversationAsync(
        int conversationId,
        Guid currentUserId
    )
    {
        var conversation = await ConversationsWithParticipants()
            .FirstAsync(c => c.Id == conversationId);
        var lastMessages = await LoadLastMessagesAsync([conversation.Id]);
        var unreadCounts = await LoadUnreadCountsAsync([conversation.Id], currentUserId);

        return conversation.ToResponseDto(
            currentUserId,
            lastMessages.GetValueOrDefault(conversation.Id),
            unreadCounts.GetValueOrDefault(conversation.Id)
        );
    }

    private IQueryable<Conversation> ConversationsWithParticipants()
    {
        return context
            .Conversations.Include(c => c.UserA)
            .Include(c => c.UserB)
            .Include(c => c.Pet);
    }

    private async Task<Dictionary<int, ChatMessage>> LoadLastMessagesAsync(
        List<int> conversationIds
    )
    {
        var lastIds = await context
            .ChatMessages.Where(m => conversationIds.Contains(m.ConversationId))
            .GroupBy(m => m.ConversationId)
            .Select(g => g.Max(m => m.Id))
            .ToListAsync();

        if (lastIds.Count == 0)
            return [];

        return await context
            .ChatMessages.Include(m => m.Sender)
            .Where(m => lastIds.Contains(m.Id))
            .ToDictionaryAsync(m => m.ConversationId);
    }

    private async Task<Dictionary<int, int>> LoadUnreadCountsAsync(
        List<int> conversationIds,
        Guid userId
    )
    {
        return await context
            .ChatMessages.Where(m =>
                conversationIds.Contains(m.ConversationId) && m.SenderId != userId && !m.IsRead
            )
            .GroupBy(m => m.ConversationId)
            .Select(g => new { ConversationId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.ConversationId, x => x.Count);
    }

    private static (Guid UserAId, Guid UserBId) OrderParticipants(Guid first, Guid second)
    {
        return first.CompareTo(second) < 0 ? (first, second) : (second, first);
    }

    private static string NormalizeContent(string content)
    {
        var normalized = content.Trim();

        if (string.IsNullOrEmpty(normalized))
            throw new ArgumentException("Message cannot be empty");

        if (normalized.Length > MaxMessageContentLength)
        {
            throw new ArgumentException(
                $"Message cannot exceed {MaxMessageContentLength} characters"
            );
        }

        return normalized;
    }
}
