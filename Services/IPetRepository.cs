using pethub.DTOs.Pet;
using pethub.Models;

namespace pethub.Services;

public interface IPetRepository
{
    Task<IEnumerable<Pet>> SearchAsync(SearchPetsQuery query);
}
