using Microsoft.AspNetCore.Mvc;
using pethub.Enums;

namespace pethub.DTOs.Pet;

public class SearchPetsQuery
{
    [FromQuery(Name = "state")]
    public string? State { get; set; }

    [FromQuery(Name = "city")]
    public string? City { get; set; }

    [FromQuery(Name = "species")]
    public string? Species { get; set; }

    [FromQuery(Name = "gender")]
    public PetGender? Gender { get; set; }

    [FromQuery(Name = "size")]
    public PetSize? Size { get; set; }

    [FromQuery(Name = "breed")]
    public string? Breed { get; set; }

    // Color tags filter - accepts multiple color names separated by comma
    // Example: ?colors=Branco,Preto
    [FromQuery(Name = "colors")]
    public string? Colors { get; set; }

    // Pattern tag filter - accepts only one pattern name
    // Example: ?pattern=Listrado
    [FromQuery(Name = "pattern")]
    public string? Pattern { get; set; }

    // Coat tag filter - accepts only one coat type
    // Example: ?coat=Curto
    [FromQuery(Name = "coat")]
    public string? Coat { get; set; }

    [FromQuery(Name = "age")]
    public AgeRangeFilter? AgeRange { get; set; }

    [FromQuery(Name = "posted")]
    public PostedDateFilter? PostedDate { get; set; }

    [FromQuery(Name = "page")]
    public int Page { get; set; } = 1;

    [FromQuery(Name = "pageSize")]
    public int PageSize { get; set; } = 10;
}
