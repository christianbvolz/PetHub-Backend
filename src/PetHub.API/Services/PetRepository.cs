using Microsoft.EntityFrameworkCore;
using pethub.Data;
using pethub.DTOs.Common;
using pethub.DTOs.Pet;
using pethub.Enums;
using pethub.Models;

namespace pethub.Services;

public class PetRepository(AppDbContext context) : IPetRepository
{
    public async Task<PagedResult<Pet>> SearchAsync(SearchPetsQuery query)
    {
        var queryable = context
            .Pets.Include(p => p.User)
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

        var items = await queryable.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

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
}
