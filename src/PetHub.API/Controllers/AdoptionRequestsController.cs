using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PetHub.API.Common;
using PetHub.API.DTOs.AdoptionRequest;
using PetHub.API.Mappings;
using PetHub.API.Services;

namespace PetHub.API.Controllers;

[ApiController]
[Route("api/adoption-requests")]
[Authorize]
public class AdoptionRequestsController(
    IAdoptionRequestRepository adoptionRequestRepository,
    IPetRepository petRepository,
    IAdoptionService adoptionService
) : ApiControllerBase
{
    /// <summary>
    /// Create a new adoption request for a pet
    /// </summary>
    [HttpPost]
    public async Task<ActionResult> CreateAdoptionRequest([FromBody] CreateAdoptionRequestDto dto)
    {
        var userIdResult = GetUserIdOrUnauthorized();
        if (userIdResult.Result is UnauthorizedObjectResult unauthorized)
            return unauthorized;

        var userId = userIdResult.Value;

        // Check if pet exists
        var pet = await petRepository.GetByIdNoTrackingAsync(dto.PetId);
        if (pet == null)
        {
            return NotFound("Pet not found");
        }

        // Check if pet is already adopted
        if (pet.IsAdopted)
        {
            return Error("Pet is already adopted");
        }

        // Check if user is the pet owner
        if (pet.UserId == userId)
        {
            return Error("You cannot request to adopt your own pet");
        }

        // Check if user already has a pending request for this pet
        var hasPending = await adoptionRequestRepository.HasPendingRequestAsync(userId, dto.PetId);
        if (hasPending)
        {
            return Error("You already have a pending adoption request for this pet");
        }

        var adoptionRequest = await adoptionRequestRepository.CreateAsync(dto, userId);

        return CreatedAtAction(
            nameof(GetAdoptionRequest),
            new { id = adoptionRequest.Id },
            adoptionRequest.ToDto()
        );
    }

    /// <summary>
    /// Get adoption request by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult> GetAdoptionRequest(int id)
    {
        var userIdResult = GetUserIdOrUnauthorized();
        if (userIdResult.Result is UnauthorizedObjectResult unauthorized)
            return unauthorized;

        var userId = userIdResult.Value;
        var adoptionRequest = await adoptionRequestRepository.GetByIdAsync(id);

        if (adoptionRequest == null)
        {
            return NotFound("Adoption request not found");
        }

        // Only adopter or pet owner can view the request
        if (adoptionRequest.AdopterId != userId && adoptionRequest.Pet?.UserId != userId)
        {
            return ForbiddenResponse("You don't have permission to view this adoption request");
        }

        return Success(adoptionRequest.ToDto());
    }

    /// <summary>
    /// Get all adoption requests made by the authenticated user
    /// </summary>
    [HttpGet("me/sent")]
    public async Task<ActionResult> GetMyRequests()
    {
        var userIdResult = GetUserIdOrUnauthorized();
        if (userIdResult.Result is UnauthorizedObjectResult unauthorized)
            return unauthorized;

        var userId = userIdResult.Value;
        var requests = await adoptionRequestRepository.GetByAdopterIdAsync(userId);

        return Success(requests.Select(r => r.ToDto()).ToList());
    }

    /// <summary>
    /// Get all adoption requests received for the authenticated user's pets
    /// </summary>
    [HttpGet("me/received")]
    public async Task<ActionResult> GetReceivedRequests()
    {
        var userIdResult = GetUserIdOrUnauthorized();
        if (userIdResult.Result is UnauthorizedObjectResult unauthorized)
            return unauthorized;

        var userId = userIdResult.Value;
        var requests = await adoptionRequestRepository.GetByPetOwnerIdAsync(userId);

        return Success(requests.Select(r => r.ToDto()).ToList());
    }

    /// <summary>
    /// Get all adoption requests for a specific pet (only pet owner can access)
    /// </summary>
    [HttpGet("pet/{petId}")]
    public async Task<ActionResult> GetPetRequests(int petId)
    {
        var userIdResult = GetUserIdOrUnauthorized();
        if (userIdResult.Result is UnauthorizedObjectResult unauthorized)
            return unauthorized;

        var userId = userIdResult.Value;

        // Check if pet exists and user is the owner
        var pet = await petRepository.GetByIdNoTrackingAsync(petId);
        if (pet == null)
        {
            return NotFound("Pet not found");
        }

        if (pet.UserId != userId)
        {
            return ForbiddenResponse("You don't have permission to view requests for this pet");
        }

        var requests = await adoptionRequestRepository.GetByPetIdAsync(petId);

        return Success(requests.Select(r => r.ToDto()).ToList());
    }

    /// <summary>
    /// Update adoption request status (only pet owner can update)
    /// </summary>
    [HttpPatch("{id}/status")]
    public async Task<ActionResult> UpdateRequestStatus(
        int id,
        [FromBody] UpdateAdoptionRequestStatusDto dto
    )
    {
        var userIdResult = GetUserIdOrUnauthorized();
        if (userIdResult.Result is UnauthorizedObjectResult unauthorized)
            return unauthorized;

        var userId = userIdResult.Value;

        var updatedRequest = await adoptionRequestRepository.UpdateStatusAsync(
            id,
            dto.Status,
            userId
        );

        if (updatedRequest == null)
        {
            return NotFound("Adoption request not found or you don't have permission to update it");
        }

        return Success(updatedRequest.ToDto(), "Adoption request status updated successfully");
    }

    /// <summary>
    /// Get all pending adoption requests for a specific pet (only pet owner can access)
    /// </summary>
    /// <param name="petId">Pet ID</param>
    /// <returns>List of pending adoption requests</returns>
    /// <response code="200">Requests retrieved successfully</response>
    /// <response code="401">User not authenticated or invalid token</response>
    /// <response code="403">User is not the owner of this pet</response>
    [HttpGet("pet/{petId}/pending")]
    public async Task<ActionResult> GetPetPendingRequests(int petId)
    {
        var userIdResult = GetUserIdOrUnauthorized();
        if (userIdResult.Result is UnauthorizedObjectResult unauthorized)
            return unauthorized;

        var userId = userIdResult.Value;

        var requests = await adoptionService.GetPetAdoptionRequestsAsync(petId, userId);

        return Success(requests.Select(r => r.ToDto()).ToList());
    }

    /// <summary>
    /// Approves a specific adoption request and marks the pet as adopted
    /// </summary>
    /// <param name="id">Adoption request ID</param>
    /// <returns>Approved adoption request data</returns>
    /// <response code="200">Request approved successfully and pet marked as adopted</response>
    /// <response code="401">User not authenticated or invalid token</response>
    /// <response code="403">User is not the owner of this pet</response>
    /// <response code="404">Adoption request not found</response>
    /// <remarks>
    /// This action will:
    /// - Mark the selected request as Approved
    /// - Mark the pet as IsAdopted = true
    /// - Reject all other pending requests for this pet
    /// </remarks>
    [HttpPost("{id}/approve")]
    public async Task<ActionResult> ApproveAdoptionRequest(int id)
    {
        var userIdResult = GetUserIdOrUnauthorized();
        if (userIdResult.Result is UnauthorizedObjectResult unauthorized)
            return unauthorized;

        var userId = userIdResult.Value;

        // Check if request exists and get ownership info
        var (exists, hasPermission) = await adoptionService.ValidateAdoptionRequestOwnershipAsync(
            id,
            userId
        );

        if (!exists)
        {
            return NotFound("Adoption request not found.");
        }

        if (!hasPermission)
        {
            return ForbiddenResponse("You don't have permission to approve this adoption request.");
        }

        var approvedRequest = await adoptionService.ApproveAdoptionRequestAsync(id, userId);

        if (approvedRequest == null)
        {
            return Error(
                "Cannot approve this adoption request. It may have already been processed or is no longer pending."
            );
        }

        return Success(
            approvedRequest.ToDto(),
            "Adoption request approved successfully. Pet marked as adopted."
        );
    }

    /// <summary>
    /// Marks a pet as adopted without selecting a specific adoption request (adopted outside platform)
    /// </summary>
    /// <param name="petId">Pet ID</param>
    /// <returns>Success confirmation</returns>
    /// <response code="200">Pet marked as adopted successfully</response>
    /// <response code="401">User not authenticated or invalid token</response>
    /// <response code="403">User is not the owner of this pet</response>
    /// <response code="404">Pet not found</response>
    /// <remarks>
    /// Use this endpoint when the pet was adopted outside the platform.
    /// This action will:
    /// - Mark the pet as IsAdopted = true
    /// - Reject all pending adoption requests for this pet
    /// </remarks>
    [HttpPost("pet/{petId}/mark-adopted")]
    public async Task<ActionResult> MarkPetAsAdopted(int petId)
    {
        var userIdResult = GetUserIdOrUnauthorized();
        if (userIdResult.Result is UnauthorizedObjectResult unauthorized)
            return unauthorized;

        var userId = userIdResult.Value;

        // Check if pet exists and validate ownership
        var (exists, hasPermission) = await adoptionService.ValidatePetOwnershipAsync(
            petId,
            userId
        );

        if (!exists)
        {
            return NotFound($"Pet with ID {petId} not found.");
        }

        if (!hasPermission)
        {
            return ForbiddenResponse("You don't have permission to mark this pet as adopted.");
        }

        await adoptionService.MarkPetAsAdoptedAsync(petId, userId);

        return Success(new { PetId = petId }, "Pet marked as adopted successfully.");
    }
}
