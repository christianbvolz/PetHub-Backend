using System.ComponentModel.DataAnnotations;
using PetHub.API.Enums;

namespace PetHub.API.DTOs.Pet;

public class CreatePetDto
{
    [MaxLength(50)]
    public string? Name { get; set; }

    [Required]
    public int SpeciesId { get; set; }

    [Required]
    public int BreedId { get; set; }

    [Required]
    public PetGender Gender { get; set; }

    [Required]
    public PetSize Size { get; set; }

    [Required]
    [Range(0, 1200)]
    public int AgeInMonths { get; set; }

    [MaxLength(1000)]
    public string? Description { get; set; }

    public bool IsCastrated { get; set; } = false;
    public bool IsVaccinated { get; set; } = false;

    [Required]
    [MinLength(1)]
    [MaxLength(6)]
    public List<string> ImageUrls { get; set; } = [];

    public List<int> TagIds { get; set; } = [];
}
