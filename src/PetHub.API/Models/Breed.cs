using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace pethub.Models;

public class Breed
{
    public int Id { get; set; }

    [Required]
    [MaxLength(50)]
    public string Name { get; set; } = string.Empty;

    // Foreign Key for Species
    public int SpeciesId { get; set; }

    [JsonIgnore]
    public Species? Species { get; set; }
}
