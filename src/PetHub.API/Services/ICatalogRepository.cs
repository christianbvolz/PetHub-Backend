using PetHub.API.Enums;
using PetHub.API.Models;

namespace PetHub.API.Services;

public interface ICatalogRepository
{
    Task<List<Species>> GetSpeciesAsync();
    Task<bool> SpeciesExistsAsync(int speciesId);
    Task<List<Breed>> GetBreedsBySpeciesIdAsync(int speciesId);
    Task<List<Tag>> GetTagsAsync(TagCategory? category);
}
