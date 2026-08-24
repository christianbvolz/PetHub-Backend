using PetHub.API.DTOs.Catalog;
using PetHub.API.Models;

namespace PetHub.API.Mappings;

public static class CatalogMappingExtensions
{
    public static SpeciesResponseDto ToResponseDto(this Species species)
    {
        return new SpeciesResponseDto { Id = species.Id, Name = species.Name };
    }

    public static BreedResponseDto ToResponseDto(this Breed breed)
    {
        return new BreedResponseDto
        {
            Id = breed.Id,
            Name = breed.Name,
            SpeciesId = breed.SpeciesId,
        };
    }

    public static TagResponseDto ToResponseDto(this Tag tag)
    {
        return new TagResponseDto
        {
            Id = tag.Id,
            Name = tag.Name,
            Category = tag.Category,
        };
    }
}
