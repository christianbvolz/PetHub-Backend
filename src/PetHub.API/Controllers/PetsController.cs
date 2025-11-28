using Microsoft.AspNetCore.Mvc;
using PetHub.API.DTOs.Common;
using PetHub.API.DTOs.Pet;
using PetHub.API.Mappings;
using PetHub.API.Services;

namespace PetHub.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PetsController(IPetRepository petRepository) : ControllerBase
{
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
}
