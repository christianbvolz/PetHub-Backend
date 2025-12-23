using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PetHub.API.Common;
using PetHub.API.DTOs.Common;
using PetHub.API.DTOs.Pet;
using PetHub.API.Mappings;
using PetHub.API.Services;

namespace PetHub.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PetsController(IPetRepository petRepository) : ApiControllerBase
{
    /// <summary>
    /// Retrieves a specific pet by ID
    /// </summary>
    /// <param name="id">Pet ID</param>
    /// <returns>Pet data</returns>
    /// <response code="200">Pet found successfully</response>
    /// <response code="404">Pet not found</response>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<PetResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<PetResponseDto>>> GetPet(int id)
    {
        var pet = await petRepository.GetByIdAsync(id);

        if (pet == null)
        {
            return NotFound($"Pet with ID {id} not found.");
        }

        return Success(pet.ToResponseDto());
    }

    /// <summary>
    /// Searches pets with filters and pagination
    /// </summary>
    /// <param name="query">Search parameters including filters by species, breed, gender, size, age, adoption status, posted date, tags and pagination</param>
    /// <returns>Paginated list of pets matching the search criteria</returns>
    /// <response code="200">Search completed successfully</response>
    [HttpGet("search")]
    [ProducesResponseType(
        typeof(ApiResponse<PagedResult<PetResponseDto>>),
        StatusCodes.Status200OK
    )]
    public async Task<ActionResult<ApiResponse<PagedResult<PetResponseDto>>>> SearchPets(
        [FromQuery] SearchPetsQuery query
    )
    {
        var pagedPets = await petRepository.SearchAsync(query);

        var result = new PagedResult<PetResponseDto>
        {
            Items = [.. pagedPets.Items.Select(p => p.ToResponseDto())],
            Page = pagedPets.Page,
            PageSize = pagedPets.PageSize,
            TotalCount = pagedPets.TotalCount,
            TotalPages = pagedPets.TotalPages,
            HasPreviousPage = pagedPets.HasPreviousPage,
            HasNextPage = pagedPets.HasNextPage,
        };

        return Success(result);
    }

    /// <summary>
    /// Creates a new pet for adoption (requires authentication)
    /// </summary>
    /// <param name="dto">Pet data including name, species, breed, gender, age, description, photos and tags</param>
    /// <returns>Created pet data</returns>
    /// <response code="201">Pet created successfully</response>
    /// <response code="400">Invalid data (species doesn't exist, breed doesn't belong to species, or invalid tags)</response>
    /// <response code="401">User not authenticated or invalid token</response>
    [HttpPost]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<PetResponseDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<PetResponseDto>>> CreatePet(CreatePetDto dto)
    {
        var userIdResult = GetUserIdOrUnauthorized();
        if (userIdResult.Result != null) // Returns 401 Unauthorized if token is invalid
            return userIdResult.Result;

        var userId = userIdResult.Value; // Extracts Guid from successful result

        // Validate Species exists
        if (!await petRepository.ValidateSpeciesExistsAsync(dto.SpeciesId))
        {
            return Error($"Species with ID {dto.SpeciesId} not found.");
        }

        // Validate Breed exists and belongs to the Species
        if (!await petRepository.ValidateBreedBelongsToSpeciesAsync(dto.BreedId, dto.SpeciesId))
        {
            return Error(
                $"Breed with ID {dto.BreedId} not found or doesn't belong to the specified species."
            );
        }

        // Validate Tags exist
        var invalidTagIds = await petRepository.ValidateTagsExistAsync(dto.TagIds);
        if (invalidTagIds.Count > 0)
        {
            return Error($"Invalid tag IDs: {string.Join(", ", invalidTagIds)}");
        }

        // Create Pet
        var pet = await petRepository.CreateAsync(dto, userId);

        return CreatedAtAction(nameof(GetPet), new { id = pet.Id }, pet.ToResponseDto());
    }

    /// <summary>
    /// Updates an existing pet (requires authentication and ownership)
    /// </summary>
    /// <param name="id">Pet ID to update</param>
    /// <param name="dto">Pet data to update (partial update supported)</param>
    /// <returns>Updated pet data</returns>
    /// <response code="200">Pet updated successfully</response>
    /// <response code="400">Invalid data (breed doesn't belong to species, or invalid tags)</response>
    /// <response code="401">User not authenticated or invalid token</response>
    /// <response code="403">User is not the owner of this pet</response>
    /// <response code="404">Pet not found</response>
    [HttpPatch("{id}")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<PetResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<PetResponseDto>>> UpdatePet(int id, UpdatePetDto dto)
    {
        var userIdResult = GetUserIdOrUnauthorized();
        if (userIdResult.Result != null) // Returns 401 Unauthorized if token is invalid
            return userIdResult.Result;

        var userId = userIdResult.Value; // Extracts Guid from successful result

        // Get pet to validate ownership and for breed validation (without tracking to avoid EF conflicts)
        var pet = await petRepository.GetByIdNoTrackingAsync(id);
        if (pet == null)
        {
            return NotFound($"Pet with ID {id} not found.");
        }

        // Validate Breed if provided
        if (dto.BreedId.HasValue)
        {
            if (
                !await petRepository.ValidateBreedBelongsToSpeciesAsync(
                    dto.BreedId.Value,
                    pet.SpeciesId
                )
            )
            {
                return Error(
                    $"Breed with ID {dto.BreedId.Value} not found or doesn't belong to the pet's species."
                );
            }
        }

        // Validate Tags if provided
        if (dto.TagIds != null && dto.TagIds.Count > 0)
        {
            var invalidTagIds = await petRepository.ValidateTagsExistAsync(dto.TagIds);
            if (invalidTagIds.Count > 0)
            {
                return Error($"Invalid tag IDs: {string.Join(", ", invalidTagIds)}");
            }
        }

        // Check ownership before update
        if (pet.UserId != userId)
        {
            return ForbiddenResponse("You don't have permission to update this pet.");
        }

        // Update Pet
        var updatedPet = await petRepository.UpdateAsync(id, dto, userId);

        if (updatedPet == null)
        {
            return Error("Failed to update pet.");
        }

        return Success(updatedPet.ToResponseDto());
    }

    /// <summary>
    /// Deletes a pet (requires authentication and ownership)
    /// </summary>
    /// <param name="id">Pet ID to delete</param>
    /// <returns>Success confirmation</returns>
    /// <response code="200">Pet deleted successfully</response>
    /// <response code="401">User not authenticated or invalid token</response>
    /// <response code="403">User is not the owner of this pet</response>
    /// <response code="404">Pet not found</response>
    [HttpDelete("{id}")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<object>>> DeletePet(int id)
    {
        var userIdResult = GetUserIdOrUnauthorized();
        if (userIdResult.Result != null) // Returns 401 Unauthorized if token is invalid
            return userIdResult.Result;

        var userId = userIdResult.Value; // Extracts Guid from successful result

        // Check if pet exists
        var pet = await petRepository.GetByIdAsync(id);
        if (pet == null)
        {
            return NotFound($"Pet with ID {id} not found.");
        }

        // Check ownership
        if (pet.UserId != userId)
        {
            return ForbiddenResponse("You don't have permission to delete this pet.");
        }

        var success = await petRepository.DeleteAsync(id, userId);

        if (!success)
        {
            return Error("Failed to delete pet.");
        }

        return Success(new { }, "Pet deleted successfully.");
    }

    /// <summary>
    /// Retrieves all pets owned by the authenticated user
    /// </summary>
    /// <returns>List of user's pets</returns>
    /// <response code="200">Pets retrieved successfully</response>
    /// <response code="401">User not authenticated or invalid token</response>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<List<PetResponseDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<List<PetResponseDto>>>> GetMyPets()
    {
        var userIdResult = GetUserIdOrUnauthorized();
        if (userIdResult.Result != null) // Returns 401 Unauthorized if token is invalid
            return userIdResult.Result;

        var userId = userIdResult.Value; // Extracts Guid from successful result

        var pets = await petRepository.GetUserPetsAsync(userId);

        return Success(pets.Select(p => p.ToResponseDto()).ToList());
    }

    /// <summary>
    /// Adds the specified pet to the authenticated user's favorites
    /// </summary>
    /// <param name="id">Pet ID</param>
    /// <returns>Success confirmation</returns>
    [HttpPost("{id}/favorite")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<object>>> AddFavorite(int id)
    {
        var userIdResult = GetUserIdOrUnauthorized();
        if (userIdResult.Result != null) // Returns 401 Unauthorized if token is invalid
            return userIdResult.Result;

        var userId = userIdResult.Value;

        var success = await petRepository.AddFavoriteAsync(userId, id);

        if (!success)
            return NotFound("Pet not found.");

        return Success(new { }, "Pet favorited successfully.");
    }

    /// <summary>
    /// Removes the specified pet from the authenticated user's favorites
    /// </summary>
    /// <param name="id">Pet ID</param>
    /// <returns>Success confirmation</returns>
    [HttpDelete("{id}/favorite")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<object>>> RemoveFavorite(int id)
    {
        var userIdResult = GetUserIdOrUnauthorized();
        if (userIdResult.Result != null) // Returns 401 Unauthorized if token is invalid
            return userIdResult.Result;

        var userId = userIdResult.Value;

        var success = await petRepository.RemoveFavoriteAsync(userId, id);

        if (!success)
            return NotFound("Favorite not found.");

        return Success(new { }, "Pet unfavorited successfully.");
    }

    /// <summary>
    /// Retrieves all favorite pets of the authenticated user
    /// </summary>
    [HttpGet("me/favorites")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<List<PetResponseDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<List<PetResponseDto>>>> GetMyFavorites()
    {
        var userIdResult = GetUserIdOrUnauthorized();
        if (userIdResult.Result != null) // Returns 401 Unauthorized if token is invalid
            return userIdResult.Result;

        var userId = userIdResult.Value;

        var pets = await petRepository.GetUserFavoritePetsAsync(userId);

        return Success(pets.Select(p => p.ToResponseDto()).ToList());
    }
}
