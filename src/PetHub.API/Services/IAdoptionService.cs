using PetHub.API.Models;

namespace PetHub.API.Services;

public interface IAdoptionService
{
    /// <summary>
    /// Get all pending adoption requests for a specific pet (owner verification included)
    /// </summary>
    Task<List<AdoptionRequest>> GetPetAdoptionRequestsAsync(int petId, Guid ownerId);

    /// <summary>
    /// Approve an adoption request, mark pet as adopted, and reject other pending requests
    /// </summary>
    Task<AdoptionRequest?> ApproveAdoptionRequestAsync(int requestId, Guid ownerId);

    /// <summary>
    /// Mark pet as adopted directly (for adoptions outside platform) and reject all pending requests
    /// </summary>
    Task<bool> MarkPetAsAdoptedAsync(int petId, Guid ownerId);

    /// <summary>
    /// Validate if an adoption request exists and if the user has permission to manage it
    /// </summary>
    Task<(bool exists, bool hasPermission)> ValidateAdoptionRequestOwnershipAsync(
        int requestId,
        Guid userId
    );

    /// <summary>
    /// Validate if a pet exists and if the user is the owner
    /// </summary>
    Task<(bool exists, bool hasPermission)> ValidatePetOwnershipAsync(int petId, Guid userId);
}
