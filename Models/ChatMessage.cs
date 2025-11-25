using System.Text.Json.Serialization;

namespace pethub.Models;

public class ChatMessage
{
    public int Id { get; set; }

    public string Content { get; set; } = string.Empty;

    public DateTime SentAt { get; set; } = DateTime.UtcNow;

    // Status: Has the receiver seen this message?
    public bool IsRead { get; set; } = false;

    // Relationships
    public int ConversationId { get; set; }

    [JsonIgnore]
    public Conversation? Conversation { get; set; }

    // Who sent this specific message?
    public int SenderId { get; set; }
}
