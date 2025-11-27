using pethub.Enums;
using pethub.Models;
using pethub.Utils;

namespace pethub.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext context)
    {
        // Check if database is already seeded
        if (context.Users.Any() || context.Pets.Any())
        {
            return; // Database already has data
        }

        // --- SEED SPECIES ---
        var species = new List<Species>
        {
            new() { Id = 1, Name = "Dog" },
            new() { Id = 2, Name = "Cat" },
            new() { Id = 3, Name = "Bird" },
            new() { Id = 4, Name = "Rabbit" },
        };
        context.Species.AddRange(species);
        await context.SaveChangesAsync();

        // --- SEED BREEDS ---
        var breeds = new List<Breed>
        {
            // Dog breeds
            new()
            {
                Id = 1,
                Name = "Golden Retriever",
                SpeciesId = 1,
            },
            new()
            {
                Id = 2,
                Name = "Labrador",
                SpeciesId = 1,
            },
            new()
            {
                Id = 3,
                Name = "Poodle",
                SpeciesId = 1,
            },
            new()
            {
                Id = 4,
                Name = "Bulldog",
                SpeciesId = 1,
            },
            new()
            {
                Id = 5,
                Name = "Beagle",
                SpeciesId = 1,
            },
            new()
            {
                Id = 6,
                Name = "German Shepherd",
                SpeciesId = 1,
            },
            new()
            {
                Id = 7,
                Name = "Pug",
                SpeciesId = 1,
            },
            new()
            {
                Id = 8,
                Name = "Chihuahua",
                SpeciesId = 1,
            },
            new()
            {
                Id = 9,
                Name = "Mixed Breed",
                SpeciesId = 1,
            },
            // Cat breeds
            new()
            {
                Id = 10,
                Name = "Persian",
                SpeciesId = 2,
            },
            new()
            {
                Id = 11,
                Name = "Siamese",
                SpeciesId = 2,
            },
            new()
            {
                Id = 12,
                Name = "Maine Coon",
                SpeciesId = 2,
            },
            new()
            {
                Id = 13,
                Name = "British Shorthair",
                SpeciesId = 2,
            },
            new()
            {
                Id = 14,
                Name = "Mixed Breed",
                SpeciesId = 2,
            },
            // Bird breeds
            new()
            {
                Id = 15,
                Name = "Parakeet",
                SpeciesId = 3,
            },
            new()
            {
                Id = 16,
                Name = "Cockatiel",
                SpeciesId = 3,
            },
            new()
            {
                Id = 17,
                Name = "Canary",
                SpeciesId = 3,
            },
            // Rabbit breeds
            new()
            {
                Id = 18,
                Name = "Holland Lop",
                SpeciesId = 4,
            },
            new()
            {
                Id = 19,
                Name = "Netherland Dwarf",
                SpeciesId = 4,
            },
        };
        context.Breeds.AddRange(breeds);
        await context.SaveChangesAsync();

        // --- SEED TAGS ---
        var tags = new List<Tag>
        {
            // Colors
            new()
            {
                Id = 1,
                Name = "White",
                Category = TagCategory.Color,
            },
            new()
            {
                Id = 2,
                Name = "Black",
                Category = TagCategory.Color,
            },
            new()
            {
                Id = 3,
                Name = "Brown",
                Category = TagCategory.Color,
            },
            new()
            {
                Id = 4,
                Name = "Golden",
                Category = TagCategory.Color,
            },
            new()
            {
                Id = 5,
                Name = "Gray",
                Category = TagCategory.Color,
            },
            new()
            {
                Id = 6,
                Name = "Orange",
                Category = TagCategory.Color,
            },
            new()
            {
                Id = 7,
                Name = "Cream",
                Category = TagCategory.Color,
            },
            // Patterns
            new()
            {
                Id = 8,
                Name = "Solid",
                Category = TagCategory.Pattern,
            },
            new()
            {
                Id = 9,
                Name = "Striped",
                Category = TagCategory.Pattern,
            },
            new()
            {
                Id = 10,
                Name = "Spotted",
                Category = TagCategory.Pattern,
            },
            new()
            {
                Id = 11,
                Name = "Bicolor",
                Category = TagCategory.Pattern,
            },
            new()
            {
                Id = 12,
                Name = "Tricolor",
                Category = TagCategory.Pattern,
            },
            // Coat types
            new()
            {
                Id = 13,
                Name = "Short",
                Category = TagCategory.Coat,
            },
            new()
            {
                Id = 14,
                Name = "Long",
                Category = TagCategory.Coat,
            },
            new()
            {
                Id = 15,
                Name = "Curly",
                Category = TagCategory.Coat,
            },
            new()
            {
                Id = 16,
                Name = "Wire",
                Category = TagCategory.Coat,
            },
        };
        context.Tags.AddRange(tags);
        await context.SaveChangesAsync();

        // --- SEED USERS ---
        var users = new List<User>
        {
            new()
            {
                Id = 1,
                Name = "christian volz",
                Email = "christianbvolz@gmail.com",
                PasswordHash = PasswordHelper.HashPassword("qwerty"),
                PhoneNumber = "11987654321",
                ZipCode = "01310100",
                State = "SP",
                City = "São Paulo",
                Neighborhood = "Centro",
                Street = "Av. Paulista",
                StreetNumber = "1000",
            },
            new()
            {
                Id = 2,
                Name = "João Santos",
                Email = "joao.santos@email.com",
                PasswordHash = PasswordHelper.HashPassword("senha123"),
                PhoneNumber = "21987654321",
                ZipCode = "20040020",
                State = "RJ",
                City = "Rio de Janeiro",
                Neighborhood = "Centro",
                Street = "Av. Rio Branco",
                StreetNumber = "500",
            },
            new()
            {
                Id = 3,
                Name = "Ana Costa",
                Email = "ana.costa@email.com",
                PasswordHash = PasswordHelper.HashPassword("senha123"),
                PhoneNumber = "31987654321",
                ZipCode = "30130100",
                State = "MG",
                City = "Belo Horizonte",
                Neighborhood = "Centro",
                Street = "Av. Afonso Pena",
                StreetNumber = "1500",
            },
        };
        context.Users.AddRange(users);
        await context.SaveChangesAsync();

        // --- SEED PETS ---
        var random = new Random();
        var pets = new List<Pet>();
        var petNames = new string[]
        {
            "Luna",
            "Max",
            "Bella",
            "Charlie",
            "Lucy",
            "Cooper",
            "Daisy",
            "Rocky",
            "Molly",
            "Buddy",
            "Sadie",
            "Tucker",
            "Bailey",
            "Maggie",
            "Jack",
            "Sophie",
            "Duke",
            "Chloe",
            "Bear",
            "Lola",
            "Zeus",
            "Penny",
            "Toby",
            "Rosie",
            "Oliver",
        };

        for (int i = 1; i <= 50; i++)
        {
            var userId = ((i - 1) % 3) + 1; // Distribute pets among 3 users
            var speciesId = random.Next(1, 3); // Only Dogs and Cats for simplicity
            var breedId =
                speciesId == 1
                    ? random.Next(1, 10) // Dog breeds
                    : random.Next(10, 15); // Cat breeds

            var pet = new Pet
            {
                Id = i,
                Name = i <= petNames.Length ? petNames[i - 1] : $"Pet{i}",
                Gender = (PetGender)random.Next(0, 3),
                Size = (PetSize)random.Next(0, 3),
                AgeInMonths = random.Next(2, 120),
                Description =
                    $"A lovely and friendly pet looking for a forever home. Very playful and gets along well with children.",
                IsCastrated = random.Next(0, 2) == 1,
                IsVaccinated = random.Next(0, 2) == 1,
                IsAdopted = false,
                UserId = userId,
                SpeciesId = speciesId,
                BreedId = breedId,
            };

            pets.Add(pet);
        }
        context.Pets.AddRange(pets);
        await context.SaveChangesAsync();

        // --- SEED PET IMAGES ---
        var petImages = new List<PetImage>();
        for (int i = 1; i <= 50; i++)
        {
            var pet = pets[i - 1];
            var imageCount = random.Next(1, 4); // 1 to 3 images per pet

            for (int imageIndex = 1; imageIndex <= imageCount; imageIndex++)
            {
                var imageUrl =
                    pet.SpeciesId == 1
                        ? "https://placedog.net/400/400?random" // Dogs
                        : "https://cataas.com/cat?width=400&height=400"; // Cats

                petImages.Add(new PetImage { PetId = i, Url = imageUrl });
            }
        }
        context.PetImages.AddRange(petImages);
        await context.SaveChangesAsync();

        // --- SEED PET TAGS ---
        var petTags = new List<PetTag>();
        for (int i = 1; i <= 50; i++)
        {
            // Add 1-2 color tags
            var colorCount = random.Next(1, 3);
            var colorIds = Enumerable.Range(1, 7).OrderBy(x => random.Next()).Take(colorCount);
            foreach (var colorId in colorIds)
            {
                petTags.Add(new PetTag { PetId = i, TagId = colorId });
            }

            // Add 1 pattern tag
            var patternId = random.Next(8, 13);
            petTags.Add(new PetTag { PetId = i, TagId = patternId });

            // Add 1 coat tag
            var coatId = random.Next(13, 17);
            petTags.Add(new PetTag { PetId = i, TagId = coatId });
        }
        context.PetTags.AddRange(petTags);
        await context.SaveChangesAsync();
    }
}
