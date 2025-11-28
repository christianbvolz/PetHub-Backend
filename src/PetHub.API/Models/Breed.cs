using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace PetHub.API.Models;

public class Breed
{
    public int Id { get; set; }

    [Required]
    [MaxLength(50)]
    public required string Name { get; set; }

    // Foreign Key for Species
    public int SpeciesId { get; set; }

    [JsonIgnore]
    public Species? Species { get; set; }
}
