using pethub.DTOs.User;
using pethub.Enums;

namespace pethub.DTOs.Pet;

public class PetResponseDto
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public string SpeciesName { get; set; } = string.Empty;
    public string BreedName { get; set; } = string.Empty;
    public PetGender Gender { get; set; }
    public PetSize Size { get; set; }
    public int AgeInMonths { get; set; }
    public string Description { get; set; } = string.Empty;
    public bool IsCastrated { get; set; }
    public bool IsVaccinated { get; set; }
    public bool IsAdopted { get; set; }
    public DateTime CreatedAt { get; set; }
    public UserResponseDto? Owner { get; set; }
    public List<TagDto> Tags { get; set; } = [];
    public List<string> ImageUrls { get; set; } = [];
}
