using Microsoft.AspNetCore.Mvc;
using PetHub.API.DTOs.Common;
using PetHub.API.DTOs.Pet;
using PetHub.API.Mappings;
using PetHub.API.Services;

namespace PetHub.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PetsController(IPetRepository petRepository, IUserRepository userRepository)
    : ControllerBase
{
    // GET: api/pets/{id}
    [HttpGet("{id}")]
    public async Task<ActionResult<PetResponseDto>> GetPet(int id)
    {
        var pet = await petRepository.GetByIdAsync(id);

        if (pet == null)
        {
            return NotFound($"Pet with ID {id} not found.");
        }

        return Ok(pet.ToResponseDto());
    }

    // GET: api/pets/search?page=1&pageSize=10
    [HttpGet("search")]
    public async Task<ActionResult<PagedResult<PetResponseDto>>> SearchPets(
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

        return Ok(result);
    }

    // POST: api/pets
    [HttpPost]
    public async Task<ActionResult<PetResponseDto>> CreatePet(CreatePetDto dto)
    {
        // TODO: Get UserId from authenticated user (for now, use first available user)
        // This is a temporary workaround for testing - replace with actual auth in production
        var users = await userRepository.GetAllAsync();
        var firstUser = users.FirstOrDefault();
        if (firstUser == null)
        {
            return BadRequest("No users found in the system. Please create a user first.");
        }
        var userId = firstUser.Id;

        // Validate Species exists
        if (!await petRepository.ValidateSpeciesExistsAsync(dto.SpeciesId))
        {
            return BadRequest($"Species with ID {dto.SpeciesId} not found.");
        }

        // Validate Breed exists and belongs to the Species
        if (!await petRepository.ValidateBreedBelongsToSpeciesAsync(dto.BreedId, dto.SpeciesId))
        {
            return BadRequest(
                $"Breed with ID {dto.BreedId} not found or doesn't belong to the specified species."
            );
        }

        // Validate Tags exist
        var invalidTagIds = await petRepository.ValidateTagsExistAsync(dto.TagIds);
        if (invalidTagIds.Count > 0)
        {
            return BadRequest($"Invalid tag IDs: {string.Join(", ", invalidTagIds)}");
        }

        // Create Pet
        var pet = await petRepository.CreateAsync(dto, userId);

        return CreatedAtAction(nameof(GetPet), new { id = pet.Id }, pet.ToResponseDto());
    }
}
