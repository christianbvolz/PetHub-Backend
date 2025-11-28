using PetHub.API.DTOs.Common;
using PetHub.API.DTOs.Pet;
using PetHub.API.Models;

namespace PetHub.API.Services;

public interface IPetRepository
{
    Task<PagedResult<Pet>> SearchAsync(SearchPetsQuery query);
}
