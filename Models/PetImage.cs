using System.Text.Json.Serialization;

namespace pethub.Models;

public class PetImage
{
    public int Id { get; set; }

    public string Url { get; set; } = string.Empty;

    // Foreign Key for Pet
    public int PetId { get; set; }

    [JsonIgnore]
    public Pet? Pet { get; set; }
}
