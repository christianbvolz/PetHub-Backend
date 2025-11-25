using System.Text.Json.Serialization;

namespace pethub.Models;

public class Pet
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    // --- FILTERS (Crucial for Adoption) ---

    // "Dog", "Cat", "Bird", etc.
    public string Species { get; set; } = string.Empty;

    // "Male" or "Female"
    public string Gender { get; set; } = string.Empty;

    // "Small", "Medium", "Large"
    public string Size { get; set; } = string.Empty;

    // Stores total months.
    // Logic: Frontend converts "2 Years" to 24 before sending.
    public int AgeInMonths { get; set; }

    public string Breed { get; set; } = string.Empty;

    public string Color { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    // --- HEALTH INFO ---
    public bool IsCastrated { get; set; } = false;
    public bool IsVaccinated { get; set; } = false;

    public bool IsAdopted { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Foreign Key for User (Owner)
    public int UserId { get; set; }

    [JsonIgnore]
    public User? User { get; set; }

    // Navigation Property
    public List<PetImage> Images { get; set; } = new();
}
