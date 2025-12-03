using PetHub.API.DTOs.Adoption;

namespace PetHub.API.Mappings;

public static class AdoptionMappingExtensions
{
    public static AdoptionRequestResponseDto ToResponseDto(this Models.AdoptionRequest request)
    {
        return new AdoptionRequestResponseDto
        {
            Id = request.Id,
            Message = request.Message,
            Status = request.Status,
            CreatedAt = request.CreatedAt,
            AdopterId = request.AdopterId,
            AdopterName = request.Adopter?.Name ?? string.Empty,
            AdopterEmail = request.Adopter?.Email,
            AdopterPhone = request.Adopter?.PhoneNumber,
            AdopterPhotoUrl = request.Adopter?.ProfilePictureUrl,
            PetId = request.PetId,
            PetName = request.Pet?.Name,
        };
    }
}
