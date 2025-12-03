using Microsoft.EntityFrameworkCore;
using PetHub.API.Data;
using PetHub.API.Enums;
using PetHub.API.Models;

namespace PetHub.API.Services;

public class AdoptionRepository(AppDbContext context) : IAdoptionRepository
{
    public async Task<List<AdoptionRequest>> GetPetAdoptionRequestsAsync(int petId, Guid ownerId)
    {
        // Verify pet ownership
        var pet = await context.Pets.FirstOrDefaultAsync(p => p.Id == petId && p.UserId == ownerId);
        if (pet == null)
            return [];

        return await context
            .AdoptionRequests.Include(ar => ar.Adopter)
            .Where(ar => ar.PetId == petId && ar.Status == AdoptionStatus.Pending)
            .OrderByDescending(ar => ar.CreatedAt)
            .ToListAsync();
    }

    public async Task<AdoptionRequest?> ApproveAdoptionRequestAsync(int requestId, Guid ownerId)
    {
        var request = await context
            .AdoptionRequests.Include(ar => ar.Pet)
            .Include(ar => ar.Adopter)
            .FirstOrDefaultAsync(ar => ar.Id == requestId && ar.Status == AdoptionStatus.Pending);

        if (request == null || request.Pet == null || request.Pet.UserId != ownerId)
            return null;

        // Approve this request
        request.Status = AdoptionStatus.Approved;

        // Mark pet as adopted
        request.Pet.IsAdopted = true;

        // Reject all other pending requests for this pet
        var otherRequests = await context
            .AdoptionRequests.Where(ar =>
                ar.PetId == request.PetId
                && ar.Id != requestId
                && ar.Status == AdoptionStatus.Pending
            )
            .ToListAsync();

        foreach (var other in otherRequests)
        {
            other.Status = AdoptionStatus.Rejected;
        }

        await context.SaveChangesAsync();

        return request;
    }

    public async Task<bool> MarkPetAsAdoptedAsync(int petId, Guid ownerId)
    {
        var pet = await context.Pets.FirstOrDefaultAsync(p => p.Id == petId && p.UserId == ownerId);

        if (pet == null)
            return false;

        // Mark pet as adopted
        pet.IsAdopted = true;

        // Reject all pending adoption requests
        var pendingRequests = await context
            .AdoptionRequests.Where(ar => ar.PetId == petId && ar.Status == AdoptionStatus.Pending)
            .ToListAsync();

        foreach (var request in pendingRequests)
        {
            request.Status = AdoptionStatus.Rejected;
        }

        await context.SaveChangesAsync();

        return true;
    }

    public async Task<(bool exists, bool hasPermission)> ValidateAdoptionRequestOwnershipAsync(
        int requestId,
        Guid userId
    )
    {
        var request = await context
            .AdoptionRequests.Include(ar => ar.Pet)
            .FirstOrDefaultAsync(ar => ar.Id == requestId);

        if (request == null)
            return (false, false);

        var hasPermission = request.Pet != null && request.Pet.UserId == userId;
        return (true, hasPermission);
    }

    public async Task<(bool exists, bool hasPermission)> ValidatePetOwnershipAsync(
        int petId,
        Guid userId
    )
    {
        var pet = await context.Pets.FirstOrDefaultAsync(p => p.Id == petId);

        if (pet == null)
            return (false, false);

        return (true, pet.UserId == userId);
    }
}
