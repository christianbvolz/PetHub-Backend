using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using PetHub.API.Enums;

namespace PetHub.API.Models;

public class Tag
{
    public int Id { get; set; }

    [Required]
    [MaxLength(50)]
    public string Name { get; set; } = string.Empty;

    [Required]
    public TagCategory Category { get; set; }

    [JsonIgnore]
    public List<PetTag> PetTags { get; set; } = [];
}
