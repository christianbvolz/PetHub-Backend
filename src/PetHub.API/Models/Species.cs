using System.ComponentModel.DataAnnotations;

namespace PetHub.API.Models;

public class Species
{
    public int Id { get; set; }

    [Required]
    [MaxLength(50)]
    public string Name { get; set; } = string.Empty;

    public List<Breed> Breeds { get; set; } = [];
}
