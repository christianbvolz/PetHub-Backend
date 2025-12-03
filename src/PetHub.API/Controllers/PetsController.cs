using System.Security.Claims;
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
        // Extract UserId from JWT token
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized("Invalid or missing user ID in token.");
        }

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
}
