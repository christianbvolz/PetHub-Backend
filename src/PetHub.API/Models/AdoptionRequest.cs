using System.Text.Json.Serialization;
using PetHub.API.Enums;

namespace PetHub.API.Models;

public class AdoptionRequest
{
    public int Id { get; set; }

    // Message from the adopter (e.g., "I have a big backyard!")
    public string Message { get; set; } = string.Empty;

    public AdoptionStatus Status { get; set; } = AdoptionStatus.Pending;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // --- RELATIONSHIPS ---

    // Which Pet?
    public int PetId { get; set; }

    [JsonIgnore]
    public Pet? Pet { get; set; }

    // Who wants to adopt?
    public Guid AdopterId { get; set; }

    [JsonIgnore]
    public User? Adopter { get; set; }
}
