using PetHub.API.Data;
using PetHub.API.Enums;
using PetHub.API.Models;
using PetHub.API.Utils;

namespace PetHub.Tests.IntegrationTests.Helpers;

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

        // Create test user with UUID v7
        var testUser = new User
        {
            Id = UuidHelper.NewId(),
            Name = TestConstants.IntegrationTests.SeededUsers.Name,
            Email = TestConstants.IntegrationTests.SeededUsers.Email,
            PasswordHash = PasswordHelper.HashPassword(
                TestConstants.IntegrationTests.SeededUsers.Password
            ),
            PhoneNumber = TestConstants.IntegrationTests.SeededUsers.PhoneNumber,
            ZipCode = TestConstants.IntegrationTests.SeededUsers.ZipCode,
            State = TestConstants.IntegrationTests.SeededUsers.State,
            City = TestConstants.IntegrationTests.SeededUsers.City,
            Neighborhood = TestConstants.IntegrationTests.SeededUsers.Neighborhood,
            Street = TestConstants.IntegrationTests.SeededUsers.Street,
            StreetNumber = TestConstants.IntegrationTests.SeededUsers.StreetNumber,
        };
        context.Users.Add(testUser);

        // Create species
        var dogSpecies = new Species { Name = TestConstants.SpeciesAndBreeds.DogNamePt };
        var catSpecies = new Species { Name = TestConstants.SpeciesAndBreeds.CatNamePt };
        context.Species.AddRange(dogSpecies, catSpecies);

        // Create breeds
        var labrador = new Breed
        {
            Name = TestConstants.SpeciesAndBreeds.LabradorName,
            Species = dogSpecies,
        };
        var poodle = new Breed
        {
            Name = TestConstants.SpeciesAndBreeds.PoodleName,
            Species = dogSpecies,
        };
        var siamese = new Breed
        {
            Name = TestConstants.SpeciesAndBreeds.SiameseName,
            Species = catSpecies,
        };
        var persian = new Breed
        {
            Name = TestConstants.SpeciesAndBreeds.PersianName,
            Species = catSpecies,
        };
        context.Breeds.AddRange(labrador, poodle, siamese, persian);

        // Create tags
        var whiteTag = new Tag
        {
            Name = TestConstants.IntegrationTests.SeededTags.White,
            Category = TagCategory.Color,
        };
        var blackTag = new Tag
        {
            Name = TestConstants.IntegrationTests.SeededTags.Black,
            Category = TagCategory.Color,
        };
        var brownTag = new Tag
        {
            Name = TestConstants.IntegrationTests.SeededTags.Brown,
            Category = TagCategory.Color,
        };
        var shortCoatTag = new Tag
        {
            Name = TestConstants.IntegrationTests.SeededTags.ShortCoat,
            Category = TagCategory.Coat,
        };
        var longCoatTag = new Tag
        {
            Name = TestConstants.IntegrationTests.SeededTags.LongCoat,
            Category = TagCategory.Coat,
        };
        context.Tags.AddRange(whiteTag, blackTag, brownTag, shortCoatTag, longCoatTag);

        await context.SaveChangesAsync();

        // Create test pets
        var pets = new List<Pet>
        {
            new()
            {
                Name = TestConstants.IntegrationTests.PetNames.Rex,
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
                Name = TestConstants.IntegrationTests.PetNames.Luna,
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
                Name = TestConstants.IntegrationTests.PetNames.Max,
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
                Name = TestConstants.IntegrationTests.PetNames.Mia,
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
                Name = TestConstants.IntegrationTests.PetNames.Bella,
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
                Name = TestConstants.IntegrationTests.PetNames.Thor,
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
