using PetHub.API.Enums;

namespace PetHub.API.DTOs.Pet;

public class TagDto
{
    public string Name { get; set; } = string.Empty;
    public TagCategory Category { get; set; }
}
