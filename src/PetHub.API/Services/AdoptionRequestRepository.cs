using Microsoft.EntityFrameworkCore;
using PetHub.API.Data;
using PetHub.API.DTOs.AdoptionRequest;
using PetHub.API.Enums;
using PetHub.API.Models;

namespace PetHub.API.Services;

public class AdoptionRequestRepository(AppDbContext context) : IAdoptionRequestRepository
{
    public async Task<AdoptionRequest> CreateAsync(CreateAdoptionRequestDto dto, Guid adopterId)
    {
        var adoptionRequest = new AdoptionRequest
        {
            PetId = dto.PetId,
            AdopterId = adopterId,
            Message = dto.Message,
            Status = AdoptionStatus.Pending,
            CreatedAt = DateTime.UtcNow,
        };

        context.AdoptionRequests.Add(adoptionRequest);
        await context.SaveChangesAsync();

        // Reload with relationships if possible; fall back to the created entity
        var reloadedAdoptionRequest = await GetByIdAsync(adoptionRequest.Id);

        return reloadedAdoptionRequest ?? adoptionRequest;
    }

    public async Task<AdoptionRequest?> GetByIdAsync(int id)
    {
        return await context
            .AdoptionRequests.AsSplitQuery()
            .Include(ar => ar.Pet)
                .ThenInclude(p => p!.User)
            .Include(ar => ar.Pet)
                .ThenInclude(p => p!.Species)
            .Include(ar => ar.Pet)
                .ThenInclude(p => p!.Breed)
            .Include(ar => ar.Pet)
                .ThenInclude(p => p!.Images)
            .Include(ar => ar.Adopter)
            .FirstOrDefaultAsync(ar => ar.Id == id);
    }

    public async Task<List<AdoptionRequest>> GetByPetIdAsync(int petId)
    {
        return await context
            .AdoptionRequests.AsSplitQuery()
            .Include(ar => ar.Pet)
                .ThenInclude(p => p!.User)
            .Include(ar => ar.Pet)
                .ThenInclude(p => p!.Species)
            .Include(ar => ar.Pet)
                .ThenInclude(p => p!.Breed)
            .Include(ar => ar.Pet)
                .ThenInclude(p => p!.Images)
            .Include(ar => ar.Adopter)
            .Where(ar => ar.PetId == petId)
            .OrderByDescending(ar => ar.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<AdoptionRequest>> GetByAdopterIdAsync(Guid adopterId)
    {
        return await context
            .AdoptionRequests.AsSplitQuery()
            .Include(ar => ar.Pet)
                .ThenInclude(p => p!.User)
            .Include(ar => ar.Pet)
                .ThenInclude(p => p!.Species)
            .Include(ar => ar.Pet)
                .ThenInclude(p => p!.Breed)
            .Include(ar => ar.Pet)
                .ThenInclude(p => p!.Images)
            .Include(ar => ar.Adopter)
            .Where(ar => ar.AdopterId == adopterId)
            .OrderByDescending(ar => ar.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<AdoptionRequest>> GetByPetOwnerIdAsync(Guid ownerId)
    {
        return await context
            .AdoptionRequests.AsSplitQuery()
            .Include(ar => ar.Pet)
                .ThenInclude(p => p!.User)
            .Include(ar => ar.Pet)
                .ThenInclude(p => p!.Species)
            .Include(ar => ar.Pet)
                .ThenInclude(p => p!.Breed)
            .Include(ar => ar.Pet)
                .ThenInclude(p => p!.Images)
            .Include(ar => ar.Adopter)
            .Where(ar => ar.Pet != null && ar.Pet.UserId == ownerId)
            .OrderByDescending(ar => ar.CreatedAt)
            .ToListAsync();
    }

    public async Task<AdoptionRequest?> UpdateStatusAsync(
        int id,
        AdoptionStatus status,
        Guid userId
    )
    {
        var adoptionRequest = await context
            .AdoptionRequests.Include(ar => ar.Pet)
            .FirstOrDefaultAsync(ar => ar.Id == id);

        if (adoptionRequest == null)
            return null;

        // Only pet owner can update status
        if (adoptionRequest.Pet?.UserId != userId)
            return null;

        adoptionRequest.Status = status;
        adoptionRequest.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync();

        // Reload with relationships
        return await GetByIdAsync(id);
    }

    public async Task<bool> HasPendingRequestAsync(Guid adopterId, int petId)
    {
        return await context.AdoptionRequests.AnyAsync(ar =>
            ar.AdopterId == adopterId && ar.PetId == petId && ar.Status == AdoptionStatus.Pending
        );
    }
}
