using pethub.DTOs.Common;
using pethub.DTOs.Pet;
using pethub.Models;

namespace pethub.Services;

public interface IPetRepository
{
    Task<PagedResult<Pet>> SearchAsync(SearchPetsQuery query);
}
