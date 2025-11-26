using System.ComponentModel.DataAnnotations;
using pethub.Enums;

namespace pethub.DTOs.Pet;

public class CreatePetDto
{
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
    [Range(0, 1200)] // 0 months to 100 years
    public int AgeInMonths { get; set; }

    [Required]
    public string Description { get; set; } = string.Empty;

    public bool IsCastrated { get; set; } = false;
    public bool IsVaccinated { get; set; } = false;

    public List<int> TagIds { get; set; } = [];

    // The list of image URLs will be provided in the request body
    public List<string> ImageUrls { get; set; } = [];
}
