using Microsoft.AspNetCore.Mvc;
using pethub.DTOs.Pet;
using pethub.Mappings;
using pethub.Services;

namespace pethub.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PetsController(IPetRepository petRepository) : ControllerBase
{
    // GET: api/pets/search
    [HttpGet("search")]
    public async Task<ActionResult<IEnumerable<PetResponseDto>>> SearchPets(
        [FromQuery] SearchPetsQuery query
    )
    {
        var pets = await petRepository.SearchAsync(query);

        return Ok(pets.Select(p => p.ToResponseDto()).ToList());
    }
}
