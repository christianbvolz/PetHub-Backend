using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using PetHub.API.Data;
using PetHub.API.DTOs.Pet;
using PetHub.API.Enums;

namespace PetHub.Tests.IntegrationTests;

/// <summary>
/// Integration tests for the CreatePet endpoint
/// Tests the complete HTTP request/response flow including database interactions
/// </summary>
public class CreatePetIntegrationTests : IClassFixture<PetHubWebApplicationFactory>, IAsyncLifetime
{
    private readonly HttpClient _client;
    private readonly PetHubWebApplicationFactory _factory;
    private int _validSpeciesId;
    private int _validBreedId;
    private int _validTagId1;
    private int _validTagId2;

    public CreatePetIntegrationTests(PetHubWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    public async Task InitializeAsync()
    {
        // Seed test data before each test class (shared across all tests)
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await dbContext.Database.EnsureDeletedAsync();
        await dbContext.Database.EnsureCreatedAsync();
        await TestDataSeeder.SeedTestData(dbContext);

        // Get valid IDs for tests
        var species = dbContext.Species.FirstOrDefault();
        var breed = dbContext.Breeds.FirstOrDefault(b => b.SpeciesId == species!.Id);
        var tags = dbContext.Tags.Take(2).ToList();

        _validSpeciesId = species?.Id ?? 0;
        _validBreedId = breed?.Id ?? 0;
        _validTagId1 = tags[0]?.Id ?? 0;
        _validTagId2 = tags[1]?.Id ?? 0;
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }

    [Fact]
    public async Task CreatePet_WithValidData_ReturnsCreated()
    {
        // Arrange
        var createDto = new CreatePetDto
        {
            Name = "Test Pet",
            SpeciesId = _validSpeciesId,
            BreedId = _validBreedId,
            Gender = PetGender.Male,
            Size = PetSize.Medium,
            AgeInMonths = 24,
            Description = "A test pet for integration testing",
            IsCastrated = true,
            IsVaccinated = true,
            ImageUrls = new List<string> { "https://example.com/pet1.jpg" },
            TagIds = new List<int> { _validTagId1, _validTagId2 },
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/pets", createDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task CreatePet_WithValidData_ReturnsCreatedPet()
    {
        // Arrange
        var createDto = new CreatePetDto
        {
            Name = "Test Pet",
            SpeciesId = _validSpeciesId,
            BreedId = _validBreedId,
            Gender = PetGender.Female,
            Size = PetSize.Small,
            AgeInMonths = 12,
            Description = "Test description",
            IsCastrated = false,
            IsVaccinated = true,
            ImageUrls = new List<string> { "https://example.com/pet2.jpg" },
            TagIds = new List<int> { _validTagId1 },
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/pets", createDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var createdPet = await response.Content.ReadFromJsonAsync<PetResponseDto>();
        createdPet.Should().NotBeNull();
        createdPet!.Id.Should().BeGreaterThan(0);
        createdPet.Name.Should().Be(createDto.Name);
        createdPet.Gender.Should().Be(createDto.Gender);
        createdPet.Size.Should().Be(createDto.Size);
        createdPet.AgeInMonths.Should().Be(createDto.AgeInMonths);
        createdPet.Description.Should().Be(createDto.Description);
        createdPet.IsCastrated.Should().Be(createDto.IsCastrated);
        createdPet.IsVaccinated.Should().Be(createDto.IsVaccinated);
    }

    [Fact]
    public async Task CreatePet_ReturnsLocationHeader()
    {
        // Arrange
        var createDto = new CreatePetDto
        {
            Name = "Location Test Pet",
            SpeciesId = _validSpeciesId,
            BreedId = _validBreedId,
            Gender = PetGender.Male,
            Size = PetSize.Large,
            AgeInMonths = 36,
            Description = "Testing location header",
            ImageUrls = new List<string> { "https://example.com/pet3.jpg" },
            TagIds = new List<int>(),
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/pets", createDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();
        response.Headers.Location!.ToString().Should().Contain("/api/Pets/");
    }

    [Fact]
    public async Task CreatePet_CreatedPetCanBeRetrieved()
    {
        // Arrange
        var createDto = new CreatePetDto
        {
            Name = "Retrievable Pet",
            SpeciesId = _validSpeciesId,
            BreedId = _validBreedId,
            Gender = PetGender.Unknown,
            Size = PetSize.Medium,
            AgeInMonths = 18,
            Description = "Can be retrieved after creation",
            ImageUrls = new List<string> { "https://example.com/pet4.jpg" },
            TagIds = new List<int> { _validTagId1 },
        };

        // Act
        var createResponse = await _client.PostAsJsonAsync("/api/pets", createDto);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var createdPet = await createResponse.Content.ReadFromJsonAsync<PetResponseDto>();
        var getResponse = await _client.GetAsync($"/api/pets/{createdPet!.Id}");

        // Assert
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var retrievedPet = await getResponse.Content.ReadFromJsonAsync<PetResponseDto>();
        retrievedPet.Should().NotBeNull();
        retrievedPet!.Id.Should().Be(createdPet.Id);
        retrievedPet.Name.Should().Be(createDto.Name);
    }

    [Fact]
    public async Task CreatePet_WithInvalidSpeciesId_ReturnsBadRequest()
    {
        // Arrange
        var createDto = new CreatePetDto
        {
            Name = "Invalid Species Pet",
            SpeciesId = 99999, // Non-existent species
            BreedId = _validBreedId,
            Gender = PetGender.Male,
            Size = PetSize.Medium,
            AgeInMonths = 24,
            ImageUrls = new List<string> { "https://example.com/pet5.jpg" },
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/pets", createDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Species");
        content.Should().Contain("not found");
    }

    [Fact]
    public async Task CreatePet_WithInvalidBreedId_ReturnsBadRequest()
    {
        // Arrange
        var createDto = new CreatePetDto
        {
            Name = "Invalid Breed Pet",
            SpeciesId = _validSpeciesId,
            BreedId = 99999, // Non-existent breed
            Gender = PetGender.Female,
            Size = PetSize.Small,
            AgeInMonths = 12,
            ImageUrls = new List<string> { "https://example.com/pet6.jpg" },
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/pets", createDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Breed");
        content.Should().Contain("not found");
    }

    [Fact]
    public async Task CreatePet_WithBreedFromDifferentSpecies_ReturnsBadRequest()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Get a breed from a different species
        var species1 = dbContext.Species.First();
        var species2 = dbContext.Species.Skip(1).First();
        var breedFromSpecies2 = dbContext.Breeds.First(b => b.SpeciesId == species2.Id);

        var createDto = new CreatePetDto
        {
            Name = "Mismatched Breed Pet",
            SpeciesId = species1.Id,
            BreedId = breedFromSpecies2.Id, // Breed from different species
            Gender = PetGender.Male,
            Size = PetSize.Medium,
            AgeInMonths = 24,
            ImageUrls = new List<string> { "https://example.com/pet7.jpg" },
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/pets", createDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Breed");
        content.Should().Contain("doesn't belong to the specified species");
    }

    [Fact]
    public async Task CreatePet_WithInvalidTagIds_ReturnsBadRequest()
    {
        // Arrange
        var createDto = new CreatePetDto
        {
            Name = "Invalid Tags Pet",
            SpeciesId = _validSpeciesId,
            BreedId = _validBreedId,
            Gender = PetGender.Male,
            Size = PetSize.Large,
            AgeInMonths = 36,
            ImageUrls = new List<string> { "https://example.com/pet8.jpg" },
            TagIds = new List<int> { _validTagId1, 99999 }, // One valid, one invalid
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/pets", createDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Invalid tag IDs");
        content.Should().Contain("99999");
    }

    [Fact]
    public async Task CreatePet_WithoutName_IsValid()
    {
        // Arrange - Name is optional (nullable)
        var createDto = new CreatePetDto
        {
            Name = null,
            SpeciesId = _validSpeciesId,
            BreedId = _validBreedId,
            Gender = PetGender.Unknown,
            Size = PetSize.Medium,
            AgeInMonths = 0, // Unknown age
            ImageUrls = new List<string> { "https://example.com/pet9.jpg" },
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/pets", createDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var createdPet = await response.Content.ReadFromJsonAsync<PetResponseDto>();
        createdPet.Should().NotBeNull();
        createdPet!.Name.Should().BeNullOrEmpty();
    }

    [Fact]
    public async Task CreatePet_WithMultipleImages_SavesAllImages()
    {
        // Arrange
        var createDto = new CreatePetDto
        {
            Name = "Multi Image Pet",
            SpeciesId = _validSpeciesId,
            BreedId = _validBreedId,
            Gender = PetGender.Female,
            Size = PetSize.Small,
            AgeInMonths = 6,
            ImageUrls = new List<string>
            {
                "https://example.com/pet10-1.jpg",
                "https://example.com/pet10-2.jpg",
                "https://example.com/pet10-3.jpg",
            },
            TagIds = new List<int>(),
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/pets", createDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var createdPet = await response.Content.ReadFromJsonAsync<PetResponseDto>();
        createdPet.Should().NotBeNull();
        createdPet!.ImageUrls.Should().HaveCount(3);
        createdPet.ImageUrls.Should().Contain("https://example.com/pet10-1.jpg");
        createdPet.ImageUrls.Should().Contain("https://example.com/pet10-2.jpg");
        createdPet.ImageUrls.Should().Contain("https://example.com/pet10-3.jpg");
    }

    [Fact]
    public async Task CreatePet_WithMultipleTags_SavesAllTags()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var allTagIds = dbContext.Tags.Select(t => t.Id).ToList();

        var createDto = new CreatePetDto
        {
            Name = "Multi Tag Pet",
            SpeciesId = _validSpeciesId,
            BreedId = _validBreedId,
            Gender = PetGender.Male,
            Size = PetSize.Large,
            AgeInMonths = 48,
            ImageUrls = new List<string> { "https://example.com/pet11.jpg" },
            TagIds = allTagIds,
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/pets", createDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var createdPet = await response.Content.ReadFromJsonAsync<PetResponseDto>();
        createdPet.Should().NotBeNull();
        createdPet!.Tags.Should().HaveCount(allTagIds.Count);
    }

    [Fact]
    public async Task CreatePet_WithNoTags_IsValid()
    {
        // Arrange
        var createDto = new CreatePetDto
        {
            Name = "No Tags Pet",
            SpeciesId = _validSpeciesId,
            BreedId = _validBreedId,
            Gender = PetGender.Female,
            Size = PetSize.Medium,
            AgeInMonths = 24,
            ImageUrls = new List<string> { "https://example.com/pet12.jpg" },
            TagIds = new List<int>(), // No tags
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/pets", createDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var createdPet = await response.Content.ReadFromJsonAsync<PetResponseDto>();
        createdPet.Should().NotBeNull();
        createdPet!.Tags.Should().BeEmpty();
    }

    [Fact]
    public async Task CreatePet_DefaultsToNotAdopted()
    {
        // Arrange
        var createDto = new CreatePetDto
        {
            Name = "Not Adopted Pet",
            SpeciesId = _validSpeciesId,
            BreedId = _validBreedId,
            Gender = PetGender.Male,
            Size = PetSize.Medium,
            AgeInMonths = 24,
            ImageUrls = new List<string> { "https://example.com/pet13.jpg" },
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/pets", createDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var createdPet = await response.Content.ReadFromJsonAsync<PetResponseDto>();
        createdPet.Should().NotBeNull();
        createdPet!.IsAdopted.Should().BeFalse();
    }

    [Fact]
    public async Task CreatePet_AssignsToHardcodedUser()
    {
        // Arrange
        // TODO: This test will need to be updated when authentication is implemented
        // Currently, the controller uses a hardcoded userId = 1
        var createDto = new CreatePetDto
        {
            Name = "User Assignment Test",
            SpeciesId = _validSpeciesId,
            BreedId = _validBreedId,
            Gender = PetGender.Female,
            Size = PetSize.Small,
            AgeInMonths = 12,
            ImageUrls = new List<string> { "https://example.com/pet14.jpg" },
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/pets", createDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var createdPet = await response.Content.ReadFromJsonAsync<PetResponseDto>();
        createdPet.Should().NotBeNull();
        createdPet!.Owner.Should().NotBeNull();
        createdPet.Owner.Id.Should().Be(1); // Hardcoded userId in controller
        createdPet.Owner.Name.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task CreatePet_WithoutAuthentication_StillSucceeds()
    {
        // Arrange
        // TODO: This test validates current behavior (no authentication required)
        // When authentication is implemented, this test should be updated to expect 401 Unauthorized
        var createDto = new CreatePetDto
        {
            Name = "No Auth Pet",
            SpeciesId = _validSpeciesId,
            BreedId = _validBreedId,
            Gender = PetGender.Male,
            Size = PetSize.Medium,
            AgeInMonths = 24,
            ImageUrls = new List<string> { "https://example.com/pet15.jpg" },
        };

        // Act - No authentication headers
        var response = await _client.PostAsJsonAsync("/api/pets", createDto);

        // Assert
        // Currently succeeds because authentication is not implemented
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        // FUTURE: When authentication is implemented, expect:
        // response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreatePet_SetsCreatedAtTimestamp()
    {
        // Arrange
        var beforeCreation = DateTime.UtcNow.AddSeconds(-5);
        var createDto = new CreatePetDto
        {
            Name = "Timestamp Test Pet",
            SpeciesId = _validSpeciesId,
            BreedId = _validBreedId,
            Gender = PetGender.Female,
            Size = PetSize.Medium,
            AgeInMonths = 18,
            ImageUrls = new List<string> { "https://example.com/pet16.jpg" },
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/pets", createDto);
        var afterCreation = DateTime.UtcNow.AddSeconds(5);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var createdPet = await response.Content.ReadFromJsonAsync<PetResponseDto>();
        createdPet.Should().NotBeNull();
        createdPet!.CreatedAt.Should().BeAfter(beforeCreation);
        createdPet.CreatedAt.Should().BeBefore(afterCreation);
    }

    [Fact]
    public async Task CreatePet_WithZeroAge_IsValid()
    {
        // Arrange - Age 0 means unknown/estimated
        var createDto = new CreatePetDto
        {
            Name = "Unknown Age Pet",
            SpeciesId = _validSpeciesId,
            BreedId = _validBreedId,
            Gender = PetGender.Unknown,
            Size = PetSize.Medium,
            AgeInMonths = 0, // Unknown age
            ImageUrls = new List<string> { "https://example.com/pet17.jpg" },
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/pets", createDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var createdPet = await response.Content.ReadFromJsonAsync<PetResponseDto>();
        createdPet.Should().NotBeNull();
        createdPet!.AgeInMonths.Should().Be(0);
    }

    [Fact]
    public async Task CreatePet_IncludesAllRelationshipsInResponse()
    {
        // Arrange
        var createDto = new CreatePetDto
        {
            Name = "Full Relationships Pet",
            SpeciesId = _validSpeciesId,
            BreedId = _validBreedId,
            Gender = PetGender.Male,
            Size = PetSize.Large,
            AgeInMonths = 36,
            Description = "Testing all relationships",
            IsCastrated = true,
            IsVaccinated = true,
            ImageUrls = new List<string> { "https://example.com/pet18.jpg" },
            TagIds = new List<int> { _validTagId1, _validTagId2 },
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/pets", createDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var createdPet = await response.Content.ReadFromJsonAsync<PetResponseDto>();
        createdPet.Should().NotBeNull();

        // Verify all relationships are loaded
        createdPet!.Owner.Should().NotBeNull();
        createdPet.Owner.Id.Should().BeGreaterThan(0);

        createdPet.SpeciesName.Should().NotBeNullOrEmpty();
        createdPet.BreedName.Should().NotBeNullOrEmpty();

        createdPet.ImageUrls.Should().NotBeEmpty();
        createdPet.ImageUrls.Should().HaveCount(1);

        createdPet.Tags.Should().NotBeEmpty();
        createdPet.Tags.Should().HaveCount(2);
        createdPet.Tags.Should().OnlyContain(t => !string.IsNullOrEmpty(t.Name));
    }
}
