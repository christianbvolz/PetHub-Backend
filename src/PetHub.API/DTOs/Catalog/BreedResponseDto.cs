namespace PetHub.API.DTOs.Catalog;

public class BreedResponseDto
{
    public required int Id { get; set; }
    public required string Name { get; set; }
    public required int SpeciesId { get; set; }
}
