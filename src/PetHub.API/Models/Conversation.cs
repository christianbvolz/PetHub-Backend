using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace PetHub.API.Models;

public class Conversation
{
    public int Id { get; set; }

    // Participant 1 (e.g., The Adopter)
    public int UserAId { get; set; }

    [JsonIgnore]
    public User? UserA { get; set; }

    // Participant 2 (e.g., The Pet Owner)
    public int UserBId { get; set; }

    [JsonIgnore]
    public User? UserB { get; set; }

    // Context: Which pet started this conversation? (Optional)
    public int? PetId { get; set; }

    [JsonIgnore]
    public Pet? Pet { get; set; }

    // Helper for sorting inbox (shows the newest chats on top)
    public DateTime LastMessageAt { get; set; } = DateTime.UtcNow;

    // Navigation to messages
    public List<ChatMessage> Messages { get; set; } = new();
}
