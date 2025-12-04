using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PetHub.API.Common;
using PetHub.API.DTOs.Adoption;
using PetHub.API.DTOs.Common;
using PetHub.API.Mappings;
using PetHub.API.Services;

namespace PetHub.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AdoptionController(IAdoptionRepository adoptionRepository) : ApiControllerBase
{
    /// <summary>
    /// Retrieves all pending adoption requests for a specific pet (owner only)
    /// </summary>
    /// <param name="petId">Pet ID</param>
    /// <returns>List of pending adoption requests</returns>
    /// <response code="200">Requests retrieved successfully</response>
    /// <response code="401">User not authenticated or invalid token</response>
    /// <response code="403">User is not the owner of this pet</response>
    [HttpGet("pets/{petId}/requests")]
    [ProducesResponseType(
        typeof(ApiResponse<List<AdoptionRequestResponseDto>>),
        StatusCodes.Status200OK
    )]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    public async Task<
        ActionResult<ApiResponse<List<AdoptionRequestResponseDto>>>
    > GetPetAdoptionRequests(int petId)
    {
        var userIdResult = GetUserIdOrUnauthorized();
        if (userIdResult.Result != null) // Returns 401 Unauthorized if token is invalid
            return userIdResult.Result;

        var userId = userIdResult.Value; // Extracts Guid from successful result

        var requests = await adoptionRepository.GetPetAdoptionRequestsAsync(petId, userId);

        return Success(requests.Select(r => r.ToResponseDto()).ToList());
    }

    /// <summary>
    /// Approves a specific adoption request and marks the pet as adopted
    /// </summary>
    /// <param name="requestId">Adoption request ID</param>
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
    [HttpPatch("requests/{requestId}/approve")]
    [ProducesResponseType(typeof(ApiResponse<AdoptionRequestResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<AdoptionRequestResponseDto>>> ApproveAdoptionRequest(
        int requestId
    )
    {
        var userIdResult = GetUserIdOrUnauthorized();
        if (userIdResult.Result != null) // Returns 401 Unauthorized if token is invalid
            return userIdResult.Result;
        var userId = userIdResult.Value; // Extracts Guid from successful result

        // Check if request exists and get ownership info
        var (exists, hasPermission) =
            await adoptionRepository.ValidateAdoptionRequestOwnershipAsync(requestId, userId);

        if (!exists)
        {
            return NotFound("Adoption request not found.");
        }

        if (!hasPermission)
        {
            return ForbiddenResponse("You don't have permission to approve this adoption request.");
        }

        var approvedRequest = await adoptionRepository.ApproveAdoptionRequestAsync(
            requestId,
            userId
        );

        if (approvedRequest == null)
        {
            return Error(
                "Cannot approve this adoption request. It may have already been processed or is no longer pending."
            );
        }

        return Success(
            approvedRequest.ToResponseDto(),
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
    [HttpPost("pets/{petId}/mark-adopted")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<object>>> MarkPetAsAdopted(int petId)
    {
        var userIdResult = GetUserIdOrUnauthorized();
        if (userIdResult.Result != null) // Returns 401 Unauthorized if token is invalid
            return userIdResult.Result;

        var userId = userIdResult.Value; // Extracts Guid from successful result

        // Check if pet exists and validate ownership
        var (exists, hasPermission) = await adoptionRepository.ValidatePetOwnershipAsync(
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

        await adoptionRepository.MarkPetAsAdoptedAsync(petId, userId);

        return Success(new { }, "Pet marked as adopted successfully.");
    }
}
