using System.ComponentModel.DataAnnotations;
using PetHub.API.Enums;

namespace PetHub.API.DTOs.Pet;

public class UpdatePetDto
{
    [MaxLength(50)]
    public string? Name { get; set; }

    public int? BreedId { get; set; }

    public PetGender? Gender { get; set; }

    public PetSize? Size { get; set; }

    [Range(0, 1200)]
    public int? AgeInMonths { get; set; }

    [MaxLength(1000)]
    public string? Description { get; set; }

    public bool? IsCastrated { get; set; }

    public bool? IsVaccinated { get; set; }

    [MaxLength(6)]
    public List<string>? ImageUrls { get; set; }

    public List<int>? TagIds { get; set; }
}
