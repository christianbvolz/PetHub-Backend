using pethub.Data;
using pethub.Enums;
using pethub.Models;

namespace PetHub.Tests.IntegrationTests;

/// <summary>
/// Helper class to seed test data for integration tests
/// </summary>
public static class TestDataSeeder
{
    public static async Task SeedTestData(AppDbContext context)
    {
        // Clear existing data
        context.Pets.RemoveRange(context.Pets);
        context.Users.RemoveRange(context.Users);
        context.Species.RemoveRange(context.Species);
        context.Breeds.RemoveRange(context.Breeds);
        context.Tags.RemoveRange(context.Tags);
        await context.SaveChangesAsync();

        // Create test user
        var testUser = new User
        {
            Name = "Test User",
            Email = "test@pethub.com",
            PasswordHash = "hashedpassword",
            PhoneNumber = "11999999999",
            ZipCode = "01000000",
            State = "SP",
            City = "São Paulo",
            Neighborhood = "Centro",
            Street = "Rua Teste",
            StreetNumber = "123",
        };
        context.Users.Add(testUser);

        // Create species
        var dogSpecies = new Species { Name = "Cachorro" };
        var catSpecies = new Species { Name = "Gato" };
        context.Species.AddRange(dogSpecies, catSpecies);

        // Create breeds
        var labrador = new Breed { Name = "Labrador", Species = dogSpecies };
        var poodle = new Breed { Name = "Poodle", Species = dogSpecies };
        var siamese = new Breed { Name = "Siamês", Species = catSpecies };
        var persian = new Breed { Name = "Persa", Species = catSpecies };
        context.Breeds.AddRange(labrador, poodle, siamese, persian);

        // Create tags
        var whiteTag = new Tag { Name = "Branco", Category = TagCategory.Color };
        var blackTag = new Tag { Name = "Preto", Category = TagCategory.Color };
        var brownTag = new Tag { Name = "Marrom", Category = TagCategory.Color };
        var shortCoatTag = new Tag { Name = "Curto", Category = TagCategory.Coat };
        var longCoatTag = new Tag { Name = "Longo", Category = TagCategory.Coat };
        context.Tags.AddRange(whiteTag, blackTag, brownTag, shortCoatTag, longCoatTag);

        await context.SaveChangesAsync();

        // Create test pets
        var pets = new List<Pet>
        {
            new()
            {
                Name = "Rex",
                Species = dogSpecies,
                Breed = labrador,
                Gender = PetGender.Male,
                Size = PetSize.Large,
                AgeInMonths = 36, // 3 years
                Description = "Friendly Labrador looking for a home",
                IsAdopted = false,
                User = testUser,
                PetTags = [new() { Tag = brownTag }, new() { Tag = shortCoatTag }],
            },
            new()
            {
                Name = "Luna",
                Species = catSpecies,
                Breed = siamese,
                Gender = PetGender.Female,
                Size = PetSize.Small,
                AgeInMonths = 18, // 1.5 years
                Description = "Beautiful Siamese cat",
                IsAdopted = false,
                User = testUser,
                PetTags = [new() { Tag = whiteTag }, new() { Tag = shortCoatTag }],
            },
            new()
            {
                Name = "Max",
                Species = dogSpecies,
                Breed = poodle,
                Gender = PetGender.Male,
                Size = PetSize.Medium,
                AgeInMonths = 6, // 6 months
                Description = "Young poodle puppy",
                IsAdopted = false,
                User = testUser,
                PetTags =
                {
                    new() { Tag = blackTag },
                    new() { Tag = longCoatTag },
                },
            },
            new()
            {
                Name = "Mia",
                Species = catSpecies,
                Breed = persian,
                Gender = PetGender.Female,
                Size = PetSize.Medium,
                AgeInMonths = 48, // 4 years
                Description = "Persian cat with long fur",
                IsAdopted = false,
                User = testUser,
                PetTags =
                {
                    new() { Tag = whiteTag },
                    new() { Tag = longCoatTag },
                },
            },
            new()
            {
                Name = "Bella",
                Species = dogSpecies,
                Breed = labrador,
                Gender = PetGender.Female,
                Size = PetSize.Medium,
                AgeInMonths = 30,
                Description = "Beautiful dog with mixed colors",
                IsAdopted = false,
                User = testUser,
                PetTags =
                {
                    new() { Tag = whiteTag },
                    new() { Tag = blackTag },
                    new() { Tag = shortCoatTag },
                },
            },
            new()
            {
                Name = "Adopted Pet",
                Species = dogSpecies,
                Breed = labrador,
                Gender = PetGender.Male,
                Size = PetSize.Large,
                AgeInMonths = 24,
                Description = "Already adopted",
                IsAdopted = true,
                User = testUser,
            },
        };

        context.Pets.AddRange(pets);
        await context.SaveChangesAsync();
    }
}
