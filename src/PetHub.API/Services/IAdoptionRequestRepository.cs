using PetHub.API.DTOs.AdoptionRequest;
using PetHub.API.Enums;
using PetHub.API.Models;

namespace PetHub.API.Services;

public interface IAdoptionRequestRepository
{
    Task<AdoptionRequest> CreateAsync(CreateAdoptionRequestDto dto, Guid adopterId);
    Task<AdoptionRequest?> GetByIdAsync(int id);
    Task<List<AdoptionRequest>> GetByPetIdAsync(int petId);
    Task<List<AdoptionRequest>> GetByAdopterIdAsync(Guid adopterId);
    Task<List<AdoptionRequest>> GetByPetOwnerIdAsync(Guid ownerId);
    Task<AdoptionRequest?> UpdateStatusAsync(int id, AdoptionStatus status, Guid userId);
    Task<bool> HasPendingRequestAsync(Guid adopterId, int petId);
}
