using System.Text.Json.Serialization;

namespace pethub.Models;

public class PetFavorite
{
    public int Id { get; set; }

    public int UserId { get; set; }

    [JsonIgnore]
    public User? User { get; set; }

    public int PetId { get; set; }

    [JsonIgnore]
    public Pet? Pet { get; set; }

    public DateTime FavoritedAt { get; set; } = DateTime.UtcNow;
}
