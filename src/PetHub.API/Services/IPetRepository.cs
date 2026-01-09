using PetHub.API.DTOs.Common;
using PetHub.API.DTOs.Pet;
using PetHub.API.Models;

namespace PetHub.API.Services;

public interface IPetRepository
{
    Task<Pet?> GetByIdAsync(int id);
    Task<Pet?> GetByIdNoTrackingAsync(int id);
    Task<PagedResult<Pet>> SearchAsync(SearchPetsQuery query);
    Task<Pet> CreateAsync(CreatePetDto dto, Guid userId);
    Task<Pet?> UpdateAsync(int id, UpdatePetDto dto, Guid userId);
    Task<bool> DeleteAsync(int id, Guid userId);
    Task<List<Pet>> GetUserPetsAsync(Guid userId);
    Task<bool> AddFavoriteAsync(Guid userId, int petId);
    Task<bool> RemoveFavoriteAsync(Guid userId, int petId);
    Task<List<Pet>> GetUserFavoritePetsAsync(Guid userId);
    Task<bool> ValidateSpeciesExistsAsync(int speciesId);
    Task<bool> ValidateBreedBelongsToSpeciesAsync(int breedId, int speciesId);
    Task<List<int>> ValidateTagsExistAsync(List<int> tagIds);
    Task<PetImage> UploadPetImageAsync(int petId, IFormFile file, Guid userId);
    Task<bool> DeletePetImageAsync(int petId, int imageId, Guid userId);
}
