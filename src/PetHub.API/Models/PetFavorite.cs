using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace PetHub.API.Models;

public class PetFavorite
{
    public int Id { get; set; }

    public Guid UserId { get; set; }

    [JsonIgnore]
    public User? User { get; set; }

    public int PetId { get; set; }

    [JsonIgnore]
    public Pet? Pet { get; set; }

    public DateTime FavoritedAt { get; private set; } = DateTime.UtcNow;
}
