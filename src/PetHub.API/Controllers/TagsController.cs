using Microsoft.AspNetCore.Mvc;
using PetHub.API.Common;
using PetHub.API.DTOs.Catalog;
using PetHub.API.DTOs.Common;
using PetHub.API.Enums;
using PetHub.API.Mappings;
using PetHub.API.Services;

namespace PetHub.API.Controllers;

[ApiController]
[Route("api/tags")]
public class TagsController(ICatalogRepository catalogRepository) : ApiControllerBase
{
    /// <summary>
    /// Lists tags used to classify pets, optionally filtered by category
    /// </summary>
    /// <param name="category">Optional category filter: Color, Pattern or Coat</param>
    /// <returns>Tags ordered by category then name</returns>
    /// <response code="200">Tags retrieved successfully</response>
    /// <response code="400">Invalid category value</response>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<TagResponseDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<List<TagResponseDto>>>> GetTags(
        [FromQuery] TagCategory? category
    )
    {
        var tags = await catalogRepository.GetTagsAsync(category);

        return Success(tags.Select(t => t.ToResponseDto()).ToList());
    }
}
