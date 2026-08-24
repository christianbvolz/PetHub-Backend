using Microsoft.AspNetCore.Mvc;
using PetHub.API.Common;
using PetHub.API.DTOs.Catalog;
using PetHub.API.DTOs.Common;
using PetHub.API.Mappings;
using PetHub.API.Services;

namespace PetHub.API.Controllers;

[ApiController]
[Route("api/species")]
public class SpeciesController(ICatalogRepository catalogRepository) : ApiControllerBase
{
    /// <summary>
    /// Lists all species available for pet listings and search filters
    /// </summary>
    /// <returns>Species catalog ordered by name</returns>
    /// <response code="200">Species retrieved successfully</response>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<SpeciesResponseDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<List<SpeciesResponseDto>>>> GetSpecies()
    {
        var species = await catalogRepository.GetSpeciesAsync();

        return Success(species.Select(s => s.ToResponseDto()).ToList());
    }

    /// <summary>
    /// Lists breeds that belong to a species
    /// </summary>
    /// <param name="id">Species ID</param>
    /// <returns>Breeds of the species ordered by name</returns>
    /// <response code="200">Breeds retrieved successfully</response>
    /// <response code="404">Species not found</response>
    [HttpGet("{id:int}/breeds")]
    [ProducesResponseType(typeof(ApiResponse<List<BreedResponseDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<List<BreedResponseDto>>>> GetBreedsBySpecies(int id)
    {
        var speciesExists = await catalogRepository.SpeciesExistsAsync(id);
        if (!speciesExists)
        {
            return NotFound($"Species with ID {id} not found.");
        }

        var breeds = await catalogRepository.GetBreedsBySpeciesIdAsync(id);

        return Success(breeds.Select(b => b.ToResponseDto()).ToList());
    }
}
