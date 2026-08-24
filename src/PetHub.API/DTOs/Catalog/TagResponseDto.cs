using PetHub.API.Enums;

namespace PetHub.API.DTOs.Catalog;

public class TagResponseDto
{
    public required int Id { get; set; }
    public required string Name { get; set; }
    public required TagCategory Category { get; set; }
}
