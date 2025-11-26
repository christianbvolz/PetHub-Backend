using pethub.DTOs.Pet;
using pethub.Models;

namespace pethub.Mappings;

public static class PetMappingExtensions
{
    public static PetResponseDto ToResponseDto(this Pet pet)
    {
        if (pet.User == null || pet.Species == null || pet.Breed == null)
        {
            throw new InvalidOperationException(
                $"As propriedades de navegação 'User', 'Species' ou 'Breed' não foram carregadas para o Pet com ID {pet.Id}. "
                    + "Certifique-se de que '.Include()' está sendo usado na consulta do repositório."
            );
        }

        var imageUrls = pet.Images?.Select(img => img.Url).ToList() ?? [];

        var tags =
            pet.PetTags?.Select(pt => new TagDto
                {
                    Name = pt.Tag?.Name ?? string.Empty,
                    Category = pt.Tag?.Category ?? default,
                })
                .ToList() ?? new List<TagDto>();

        return new PetResponseDto
        {
            Id = pet.Id,
            Name = pet.Name,
            SpeciesName = pet.Species.Name,
            BreedName = pet.Breed.Name,
            Gender = pet.Gender,
            Size = pet.Size,
            AgeInMonths = pet.AgeInMonths,
            Description = pet.Description,
            IsCastrated = pet.IsCastrated,
            IsVaccinated = pet.IsVaccinated,
            IsAdopted = pet.IsAdopted,
            CreatedAt = pet.CreatedAt,
            Owner = pet.User.ToResponseDto(),
            ImageUrls = imageUrls,
            Tags = tags,
        };
    }
}
