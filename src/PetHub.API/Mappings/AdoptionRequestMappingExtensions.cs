using PetHub.API.DTOs.AdoptionRequest;
using PetHub.API.Models;

namespace PetHub.API.Mappings;

public static class AdoptionRequestMappingExtensions
{
    public static AdoptionRequestResponseDto ToDto(this AdoptionRequest adoptionRequest)
    {
        return new AdoptionRequestResponseDto
        {
            Id = adoptionRequest.Id,
            PetId = adoptionRequest.PetId,
            AdopterId = adoptionRequest.AdopterId,
            Message = adoptionRequest.Message,
            Status = adoptionRequest.Status,
            CreatedAt = adoptionRequest.CreatedAt,
            UpdatedAt = adoptionRequest.UpdatedAt,
            Pet = adoptionRequest.Pet?.ToResponseDto(),
            Adopter = adoptionRequest.Adopter?.ToResponseDto(),
        };
    }
}
