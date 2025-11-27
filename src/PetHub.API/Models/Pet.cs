using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using pethub.Enums;

namespace pethub.Models;

public class Pet
{
    public int Id { get; set; }

    [MaxLength(50)]
    public string? Name { get; set; }

    [Required]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public PetGender Gender { get; set; }

    [Required]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public PetSize Size { get; set; }

    [Required]
    public int AgeInMonths { get; set; }

    [MaxLength(1000)]
    public string? Description { get; set; }

    // --- HEALTH INFO ---
    public bool IsCastrated { get; set; } = false;
    public bool IsVaccinated { get; set; } = false;

    public bool IsAdopted { get; set; } = false;

    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

    // --- RELATIONSHIPS ---

    // Foreign Key for User (Owner)
    [Required]
    public int UserId { get; set; }

    [JsonIgnore]
    public User? User { get; set; }

    // Foreign Key for Species
    [Required]
    public int SpeciesId { get; set; }

    [JsonIgnore]
    public Species? Species { get; set; }

    // Foreign Key for Breed
    [Required]
    public int BreedId { get; set; }

    [JsonIgnore]
    public Breed? Breed { get; set; }

    public List<PetTag> PetTags { get; set; } = [];

    // Navigation Property
    public List<PetImage> Images { get; set; } = [];
}
