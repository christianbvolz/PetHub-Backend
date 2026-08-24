using Microsoft.EntityFrameworkCore;
using PetHub.API.Data;
using PetHub.API.Enums;
using PetHub.API.Models;

namespace PetHub.API.Services;

public class AdoptionService(
    AppDbContext context,
    IAdoptionRequestRepository adoptionRequestRepository,
    IPetRepository petRepository,
    INotificationService notificationService,
    ILogger<AdoptionService> logger
) : IAdoptionService
{
    public async Task<List<AdoptionRequest>> GetPetAdoptionRequestsAsync(int petId, Guid ownerId)
    {
        // Verify pet ownership
        var pet = await petRepository.GetByIdNoTrackingAsync(petId);
        if (pet == null)
            throw new KeyNotFoundException($"Pet with id {petId} was not found.");

        if (pet.UserId != ownerId)
            throw new UnauthorizedAccessException(
                "You do not have permission to view adoption requests for this pet."
            );
        // Get all pending requests for this pet directly from the database
        return await context
            .AdoptionRequests.Include(ar => ar.Pet)
            .Include(ar => ar.Adopter)
            .Where(ar => ar.PetId == petId && ar.Status == AdoptionStatus.Pending)
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

        List<AdoptionRequest> otherRequests = [];
        AdoptionRequest? approved;

        // Start transaction for atomic operations
        using var transaction = await context.Database.BeginTransactionAsync();

        try
        {
            // Approve this request
            request.Status = AdoptionStatus.Approved;
            request.UpdatedAt = DateTime.UtcNow;

            // Mark pet as adopted
            request.Pet.IsAdopted = true;

            // Reject all other pending requests for this pet
            otherRequests = await context
                .AdoptionRequests.Include(ar => ar.Pet)
                .Include(ar => ar.Adopter)
                .Where(ar =>
                    ar.PetId == request.PetId
                    && ar.Id != requestId
                    && ar.Status == AdoptionStatus.Pending
                )
                .ToListAsync();

            foreach (var other in otherRequests)
            {
                other.Status = AdoptionStatus.Rejected;
                other.UpdatedAt = DateTime.UtcNow;
            }

            await context.SaveChangesAsync();
            await transaction.CommitAsync();

            approved = await adoptionRequestRepository.GetByIdAsync(requestId);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Failed to approve adoption request {RequestId} by owner {OwnerId}",
                requestId,
                ownerId
            );
            await transaction.RollbackAsync();
            throw;
        }

        if (approved != null)
        {
            await notificationService.NotifyAdoptionEventAsync(
                NotificationType.AdoptionRequestApproved,
                approved.AdopterId,
                approved
            );
        }

        foreach (var other in otherRequests)
        {
            await notificationService.NotifyAdoptionEventAsync(
                NotificationType.AdoptionRequestRejected,
                other.AdopterId,
                other
            );
        }

        return approved;
    }

    public async Task<AdoptionRequest?> CancelAdoptionRequestAsync(int requestId, Guid adopterId)
    {
        var request = await context
            .AdoptionRequests.Include(ar => ar.Pet)
            .Include(ar => ar.Adopter)
            .FirstOrDefaultAsync(ar => ar.Id == requestId);

        if (request == null)
            return null;

        if (request.AdopterId != adopterId)
            throw new UnauthorizedAccessException(
                "You don't have permission to cancel this adoption request."
            );

        if (request.Status != AdoptionStatus.Pending)
            throw new ArgumentException("Only pending adoption requests can be cancelled.");

        request.Status = AdoptionStatus.Cancelled;
        request.UpdatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();

        var cancelled = await adoptionRequestRepository.GetByIdAsync(requestId);
        if (cancelled?.Pet != null)
        {
            await notificationService.NotifyAdoptionEventAsync(
                NotificationType.AdoptionRequestCancelled,
                cancelled.Pet.UserId,
                cancelled
            );
        }

        return cancelled;
    }

    public async Task<bool> MarkPetAsAdoptedAsync(int petId, Guid ownerId)
    {
        var pet = await context.Pets.FirstOrDefaultAsync(p => p.Id == petId && p.UserId == ownerId);

        if (pet == null)
            return false;

        List<AdoptionRequest> pendingRequests = [];

        // Start transaction for atomic operations
        using var transaction = await context.Database.BeginTransactionAsync();

        try
        {
            // Mark pet as adopted
            pet.IsAdopted = true;

            // Reject all pending adoption requests
            pendingRequests = await context
                .AdoptionRequests.Include(ar => ar.Pet)
                .Include(ar => ar.Adopter)
                .Where(ar => ar.PetId == petId && ar.Status == AdoptionStatus.Pending)
                .ToListAsync();

            foreach (var request in pendingRequests)
            {
                request.Status = AdoptionStatus.Rejected;
                request.UpdatedAt = DateTime.UtcNow;
            }

            await context.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Failed to mark pet {PetId} as adopted by owner {OwnerId}",
                petId,
                ownerId
            );
            await transaction.RollbackAsync();
            throw;
        }

        foreach (var request in pendingRequests)
        {
            await notificationService.NotifyAdoptionEventAsync(
                NotificationType.AdoptionRequestRejected,
                request.AdopterId,
                request
            );
        }

        return true;
    }

    public async Task<(bool exists, bool hasPermission)> ValidateAdoptionRequestOwnershipAsync(
        int requestId,
        Guid userId
    )
    {
        var request = await adoptionRequestRepository.GetByIdAsync(requestId);

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
        var pet = await petRepository.GetByIdNoTrackingAsync(petId);

        if (pet == null)
            return (false, false);

        return (true, pet.UserId == userId);
    }
}
