using pethub.DTOs.User;
using pethub.Enums;

namespace pethub.DTOs.Pet;

public class PetResponseDto
{
    public required int Id { get; set; }
    public string? Name { get; set; }
    public required string SpeciesName { get; set; }
    public required string BreedName { get; set; }
    public required PetGender Gender { get; set; }
    public required PetSize Size { get; set; }
    public required int AgeInMonths { get; set; }
    public required string Description { get; set; }
    public required bool IsCastrated { get; set; }
    public required bool IsVaccinated { get; set; }
    public required bool IsAdopted { get; set; }
    public required DateTime CreatedAt { get; set; }
    public required UserResponseDto Owner { get; set; }
    public required List<TagDto> Tags { get; set; }
    public required List<string> ImageUrls { get; set; }
}
