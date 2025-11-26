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

    // Tags filter - accepts multiple tag names separated by comma
    // Example: ?tags=Branco,Listrado
    [FromQuery(Name = "tags")]
    public string? Tags { get; set; }

    [FromQuery(Name = "age")]
    public AgeRangeFilter? AgeRange { get; set; }

    [FromQuery(Name = "posted")]
    public PostedDateFilter? PostedDate { get; set; }
}
