using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace PetHub.API.Models;

public class ChatMessage
{
    public int Id { get; set; }

    [MaxLength(2000)]
    public string Content { get; set; } = string.Empty;

    public DateTime SentAt { get; set; } = DateTime.UtcNow;

    // Status: Has the receiver seen this message?
    public bool IsRead { get; set; } = false;

    // Relationships
    public int ConversationId { get; set; }

    [JsonIgnore]
    public Conversation? Conversation { get; set; }

    // Who sent this specific message?
    public Guid SenderId { get; set; }

    [JsonIgnore]
    public User? Sender { get; set; }
}
