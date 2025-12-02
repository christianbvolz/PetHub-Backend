using PetHub.API.DTOs.Common;
using PetHub.API.DTOs.Pet;
using PetHub.API.Models;

namespace PetHub.API.Services;

public interface IPetRepository
{
    Task<Pet?> GetByIdAsync(int id);
    Task<PagedResult<Pet>> SearchAsync(SearchPetsQuery query);
    Task<Pet> CreateAsync(CreatePetDto dto, Guid userId);
    Task<bool> ValidateSpeciesExistsAsync(int speciesId);
    Task<bool> ValidateBreedBelongsToSpeciesAsync(int breedId, int speciesId);
    Task<List<int>> ValidateTagsExistAsync(List<int> tagIds);
}
