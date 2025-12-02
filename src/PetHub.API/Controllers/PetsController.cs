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
    // GET: api/pets/{id}
    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<PetResponseDto>>> GetPet(int id)
    {
        var pet = await petRepository.GetByIdAsync(id);

        if (pet == null)
        {
            return NotFound($"Pet with ID {id} not found.");
        }

        return Success(pet.ToResponseDto());
    }

    // GET: api/pets/search?page=1&pageSize=10
    [HttpGet("search")]
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

    // POST: api/pets
    [HttpPost]
    [Authorize]
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
