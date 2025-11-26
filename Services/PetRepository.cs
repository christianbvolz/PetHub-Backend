using Microsoft.EntityFrameworkCore;
using pethub.Data;
using pethub.DTOs.Pet;
using pethub.Enums;
using pethub.Models;

namespace pethub.Services;

public class PetRepository(AppDbContext context) : IPetRepository
{
    public async Task<IEnumerable<Pet>> SearchAsync(SearchPetsQuery query)
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

        // --- TAGS FILTER ---
        if (!string.IsNullOrWhiteSpace(query.Tags))
        {
            var tagNames = query
                .Tags.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(t => t.Trim())
                .ToList();

            if (tagNames.Count != 0)
            {
                foreach (var tagName in tagNames)
                {
                    queryable = queryable.Where(p =>
                        p.PetTags.Any(pt => pt.Tag != null && pt.Tag.Name == tagName)
                    );
                }
            }
        }

        return await queryable.ToListAsync();
    }
}
