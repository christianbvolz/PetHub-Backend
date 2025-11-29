using PetHub.API.DTOs.Common;
using PetHub.API.DTOs.Pet;
using PetHub.API.Models;

namespace PetHub.API.Services;

public interface IPetRepository
{
    public Task<Pet?> GetByIdAsync(int id);
    public Task<PagedResult<Pet>> SearchAsync(SearchPetsQuery query);
    public Task<Pet> CreateAsync(CreatePetDto dto, Guid userId);
    public Task<bool> ValidateSpeciesExistsAsync(int speciesId);
    public Task<bool> ValidateBreedBelongsToSpeciesAsync(int breedId, int speciesId);
    public Task<List<int>> ValidateTagsExistAsync(List<int> tagIds);
}
