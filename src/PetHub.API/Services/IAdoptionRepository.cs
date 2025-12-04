using PetHub.API.Models;

namespace PetHub.API.Services;

public interface IAdoptionRepository
{
    Task<List<AdoptionRequest>> GetPetAdoptionRequestsAsync(int petId, Guid ownerId);
    Task<AdoptionRequest?> ApproveAdoptionRequestAsync(int requestId, Guid ownerId);
    Task<bool> MarkPetAsAdoptedAsync(int petId, Guid ownerId);
    Task<(bool exists, bool hasPermission)> ValidateAdoptionRequestOwnershipAsync(
        int requestId,
        Guid userId
    );
    Task<(bool exists, bool hasPermission)> ValidatePetOwnershipAsync(int petId, Guid userId);
}
