using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace PetHub.API.Models;

public class PetImage
{
    public int Id { get; set; }

    [Required]
    [MaxLength(2048)]
    public string Url { get; set; } = string.Empty;

    // Foreign Key for Pet
    public int PetId { get; set; }

    [JsonIgnore]
    public Pet? Pet { get; set; }
}
