using Microsoft.AspNetCore.Mvc;
using pethub.DTOs.Common;
using pethub.DTOs.Pet;
using pethub.Mappings;
using pethub.Services;

namespace pethub.Controllers;

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
