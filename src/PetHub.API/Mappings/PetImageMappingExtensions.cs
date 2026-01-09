using PetHub.API.Models;

namespace PetHub.API.Mappings;

public static class PetImageMappingExtensions
{
    public static DTOs.PetImage.PetImageResponseDto ToDto(this PetImage petImage)
    {
        return new DTOs.PetImage.PetImageResponseDto
        {
            Id = petImage.Id,
            Url = petImage.Url,
            PetId = petImage.PetId,
        };
    }
}
