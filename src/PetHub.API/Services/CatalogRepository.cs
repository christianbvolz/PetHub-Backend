using Microsoft.EntityFrameworkCore;
using PetHub.API.Data;
using PetHub.API.Enums;
using PetHub.API.Models;

namespace PetHub.API.Services;

public class CatalogRepository(AppDbContext context) : ICatalogRepository
{
    public async Task<List<Species>> GetSpeciesAsync()
    {
        return await context.Species.AsNoTracking().OrderBy(s => s.Name).ToListAsync();
    }

    public async Task<bool> SpeciesExistsAsync(int speciesId)
    {
        return await context.Species.AsNoTracking().AnyAsync(s => s.Id == speciesId);
    }

    public async Task<List<Breed>> GetBreedsBySpeciesIdAsync(int speciesId)
    {
        return await context
            .Breeds.AsNoTracking()
            .Where(b => b.SpeciesId == speciesId)
            .OrderBy(b => b.Name)
            .ToListAsync();
    }

    public async Task<List<Tag>> GetTagsAsync(TagCategory? category)
    {
        var query = context.Tags.AsNoTracking().AsQueryable();

        if (category.HasValue)
        {
            query = query.Where(t => t.Category == category.Value);
        }

        return await query.OrderBy(t => t.Category).ThenBy(t => t.Name).ToListAsync();
    }
}
