using Microsoft.EntityFrameworkCore;
using PetHub.API.Data;
using PetHub.API.DTOs.Common;
using PetHub.API.DTOs.Pet;
using PetHub.API.Enums;
using PetHub.API.Models;

namespace PetHub.API.Services;

public class PetRepository(AppDbContext context) : IPetRepository
{
    public async Task<Pet?> GetByIdAsync(int id)
    {
        return await context
            .Pets.AsSplitQuery()
            .Include(p => p.User)
            .Include(p => p.Species)
            .Include(p => p.Breed)
            .Include(p => p.Images)
            .Include(p => p.PetTags)
                .ThenInclude(pt => pt.Tag)
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<Pet?> GetByIdNoTrackingAsync(int id)
    {
        return await context
            .Pets.AsNoTracking()
            .AsSplitQuery()
            .Include(p => p.User)
            .Include(p => p.Species)
            .Include(p => p.Breed)
            .Include(p => p.Images)
            .Include(p => p.PetTags)
                .ThenInclude(pt => pt.Tag)
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<PagedResult<Pet>> SearchAsync(SearchPetsQuery query)
    {
        var queryable = context
            .Pets.AsSplitQuery()
            .Include(p => p.User)
            .Include(p => p.Species)
            .Include(p => p.Breed)
            .Include(p => p.Images)
            .Include(p => p.PetTags)
                .ThenInclude(pt => pt.Tag)
            .Where(p => !p.IsAdopted);

        // --- LOCATION FILTERS (OWNER) ---
        if (!string.IsNullOrWhiteSpace(query.State))
        {
            queryable = queryable.Where(p => p.User != null && p.User.State == query.State);
        }
        if (!string.IsNullOrWhiteSpace(query.City))
        {
            queryable = queryable.Where(p => p.User != null && p.User.City == query.City);
        }

        // --- PET ATTRIBUTE FILTERS ---
        if (!string.IsNullOrWhiteSpace(query.Species))
        {
            queryable = queryable.Where(p => p.Species != null && p.Species.Name == query.Species);
        }
        if (query.Gender.HasValue)
        {
            queryable = queryable.Where(p => p.Gender == query.Gender.Value);
        }
        if (query.Size.HasValue)
        {
            queryable = queryable.Where(p => p.Size == query.Size.Value);
        }
        if (!string.IsNullOrWhiteSpace(query.Breed))
        {
            queryable = queryable.Where(p => p.Breed != null && p.Breed.Name.Contains(query.Breed));
        }

        // --- AGE FILTER (RANGE) ---
        if (query.AgeRange.HasValue)
        {
            var (minAge, maxAge) = query.AgeRange.Value switch
            {
                AgeRangeFilter.Baby => (0, 11),
                AgeRangeFilter.Young => (12, 36),
                AgeRangeFilter.Adult => (36, 96),
                _ => (0, int.MaxValue),
            };

            queryable = queryable.Where(p => p.AgeInMonths >= minAge && p.AgeInMonths <= maxAge);
        }

        // --- POSTING DATE FILTER ---
        if (query.PostedDate.HasValue)
        {
            var today = DateTime.UtcNow.Date;

            queryable = query.PostedDate.Value switch
            {
                PostedDateFilter.Today => queryable.Where(p => p.CreatedAt.Date == today),
                PostedDateFilter.ThisWeek => queryable.Where(p =>
                    p.CreatedAt.Date >= today.AddDays(-(int)today.DayOfWeek)
                ),
                PostedDateFilter.ThisMonth => queryable.Where(p =>
                    p.CreatedAt.Date >= new DateTime(today.Year, today.Month, 1)
                ),
                PostedDateFilter.ThisYear => queryable.Where(p =>
                    p.CreatedAt.Date >= new DateTime(today.Year, 1, 1)
                ),
                _ => queryable,
            };
        }

        // --- COLOR TAGS FILTER (multiple colors with AND) ---
        if (!string.IsNullOrWhiteSpace(query.Colors))
        {
            var colorNames = query
                .Colors.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(c => c.Trim())
                .ToList();

            if (colorNames.Count != 0)
            {
                // Use AND logic: pet must have ALL specified colors
                foreach (var colorName in colorNames)
                {
                    queryable = queryable.Where(p =>
                        p.PetTags.Any(pt =>
                            pt.Tag != null
                            && pt.Tag.Name == colorName
                            && pt.Tag.Category == TagCategory.Color
                        )
                    );
                }
            }
        }

        // --- PATTERN TAG FILTER (single pattern only) ---
        if (!string.IsNullOrWhiteSpace(query.Pattern))
        {
            queryable = queryable.Where(p =>
                p.PetTags.Any(pt =>
                    pt.Tag != null
                    && pt.Tag.Name == query.Pattern
                    && pt.Tag.Category == TagCategory.Pattern
                )
            );
        }

        // --- COAT TAG FILTER (apenas um tipo de pelagem) ---
        if (!string.IsNullOrWhiteSpace(query.Coat))
        {
            queryable = queryable.Where(p =>
                p.PetTags.Any(pt =>
                    pt.Tag != null
                    && pt.Tag.Name == query.Coat
                    && pt.Tag.Category == TagCategory.Coat
                )
            );
        }

        // --- PAGINATION ---
        // Ensure page is at least 1
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Max(1, Math.Min(100, query.PageSize)); // Max 100 items per page

        // Execute queries sequentially to avoid DbContext concurrency issues
        var totalCount = await queryable.CountAsync();
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        var items = await queryable
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<Pet>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = totalPages,
            HasPreviousPage = page > 1,
            HasNextPage = page < totalPages,
        };
    }

    public async Task<Pet> CreateAsync(CreatePetDto dto, Guid userId)
    {
        var pet = new Pet
        {
            Name = dto.Name,
            SpeciesId = dto.SpeciesId,
            BreedId = dto.BreedId,
            Gender = dto.Gender,
            Size = dto.Size,
            AgeInMonths = dto.AgeInMonths,
            Description = dto.Description,
            IsCastrated = dto.IsCastrated,
            IsVaccinated = dto.IsVaccinated,
            UserId = userId,
        };

        context.Pets.Add(pet);
        await context.SaveChangesAsync();

        // Add Images
        if (dto.ImageUrls.Count > 0)
        {
            var images = dto
                .ImageUrls.Select(url => new PetImage { PetId = pet.Id, Url = url })
                .ToList();

            context.PetImages.AddRange(images);
        }

        // Add Tags
        if (dto.TagIds.Count > 0)
        {
            var petTags = dto
                .TagIds.Select(tagId => new PetTag { PetId = pet.Id, TagId = tagId })
                .ToList();

            context.PetTags.AddRange(petTags);
        }

        await context.SaveChangesAsync();

        // Reload pet with all relationships
        return (await GetByIdAsync(pet.Id))!;
    }

    public async Task<bool> ValidateSpeciesExistsAsync(int speciesId)
    {
        return await context.Species.AnyAsync(s => s.Id == speciesId);
    }

    public async Task<bool> ValidateBreedBelongsToSpeciesAsync(int breedId, int speciesId)
    {
        return await context.Breeds.AnyAsync(b => b.Id == breedId && b.SpeciesId == speciesId);
    }

    public async Task<List<int>> ValidateTagsExistAsync(List<int> tagIds)
    {
        if (tagIds.Count == 0)
            return [];

        var existingTagIds = await context
            .Tags.Where(t => tagIds.Contains(t.Id))
            .Select(t => t.Id)
            .ToListAsync();

        return [.. tagIds.Except(existingTagIds)];
    }

    public async Task<Pet?> UpdateAsync(int id, UpdatePetDto dto, Guid userId)
    {
        // Note: Ownership validation is now handled in the controller before calling this method
        // This method assumes the pet exists and the user has permission to update it

        // Load the pet with tracking for update. The controller has already validated ownership
        // using GetByIdNoTrackingAsync, so here we only need to load and modify the entity.
        var pet = await context
            .Pets.Include(p => p.Images)
            .Include(p => p.PetTags)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (pet == null)
            return null;

        // Update fields only if provided
        if (dto.Name != null)
            pet.Name = dto.Name;
        if (dto.BreedId.HasValue)
            pet.BreedId = dto.BreedId.Value;
        if (dto.Gender.HasValue)
            pet.Gender = dto.Gender.Value;
        if (dto.Size.HasValue)
            pet.Size = dto.Size.Value;
        if (dto.AgeInMonths.HasValue)
            pet.AgeInMonths = dto.AgeInMonths.Value;
        if (dto.Description != null)
            pet.Description = dto.Description;
        if (dto.IsCastrated.HasValue)
            pet.IsCastrated = dto.IsCastrated.Value;
        if (dto.IsVaccinated.HasValue)
            pet.IsVaccinated = dto.IsVaccinated.Value;

        // Update Images if provided
        if (dto.ImageUrls != null)
        {
            // Remove existing images.
            // Design decision: We replace all images rather than performing a delta update for simplicity and consistency.
            // Note: For pets with a large number of images, this approach may have performance implications due to increased database operations.
            context.PetImages.RemoveRange(pet.Images);

            // Add new images
            if (dto.ImageUrls.Count > 0)
            {
                var images = dto
                    .ImageUrls.Select(url => new PetImage { PetId = pet.Id, Url = url })
                    .ToList();
                context.PetImages.AddRange(images);
            }
        }

        // Update Tags if provided
        // This replaces all existing tags with the new set provided in the DTO.
        // This approach is chosen for simplicity, avoiding complex diff logic.
        // Note: For pets with many tags, this may have performance implications.
        if (dto.TagIds != null)
        {
            // Remove existing tags
            context.PetTags.RemoveRange(pet.PetTags);

            // Add new tags
            if (dto.TagIds.Count > 0)
            {
                var petTags = dto
                    .TagIds.Select(tagId => new PetTag { PetId = pet.Id, TagId = tagId })
                    .ToList();
                context.PetTags.AddRange(petTags);
            }
        }

        await context.SaveChangesAsync();

        // Reload pet with all relationships
        return await GetByIdAsync(id);
    }

    public async Task<bool> DeleteAsync(int id, Guid userId)
    {
        var pet = await context.Pets.FirstOrDefaultAsync(p => p.Id == id);

        if (pet == null)
            return false;

        context.Pets.Remove(pet);
        await context.SaveChangesAsync();

        return true;
    }

    public async Task<List<Pet>> GetUserPetsAsync(Guid userId)
    {
        return await context
            .Pets.AsSplitQuery()
            .Include(p => p.User)
            .Include(p => p.Species)
            .Include(p => p.Breed)
            .Include(p => p.Images)
            .Include(p => p.PetTags)
                .ThenInclude(pt => pt.Tag)
            .Where(p => p.UserId == userId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
    }

    public async Task<bool> AddFavoriteAsync(Guid userId, int petId)
    {
        // Ensure pet exists
        var pet = await context.Pets.FindAsync(petId);
        if (pet == null)
            return false;

        // Check if already favorited
        var exists = await context.PetFavorites.AnyAsync(pf =>
            pf.UserId == userId && pf.PetId == petId
        );
        if (exists)
            return true; // idempotent

        var favorite = new PetFavorite { UserId = userId, PetId = petId };
        context.PetFavorites.Add(favorite);
        await context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> RemoveFavoriteAsync(Guid userId, int petId)
    {
        var fav = await context.PetFavorites.FirstOrDefaultAsync(pf =>
            pf.UserId == userId && pf.PetId == petId
        );
        if (fav == null)
            return false;

        context.PetFavorites.Remove(fav);
        await context.SaveChangesAsync();
        return true;
    }

    public async Task<List<Pet>> GetUserFavoritePetsAsync(Guid userId)
    {
        return await context
            .Pets.Join(
                context.PetFavorites.Where(pf => pf.UserId == userId),
                p => p.Id,
                pf => pf.PetId,
                (p, pf) => p
            )
            .Include(p => p.User)
            .Include(p => p.Species)
            .Include(p => p.Breed)
            .Include(p => p.Images)
            .Include(p => p.PetTags)
                .ThenInclude(pt => pt.Tag)
            .AsSplitQuery()
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
    }
}
