using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using PetHub.API.Data;
using PetHub.API.DTOs.Common;
using PetHub.API.DTOs.Pet;
using PetHub.API.Enums;
using PetHub.Tests;
using PetHub.Tests.Extensions;
using PetHub.Tests.IntegrationTests.Helpers;
using PetHub.Tests.IntegrationTests.Infrastructure;

namespace PetHub.Tests.IntegrationTests.Controllers.PetsControllerTests;

/// <summary>
/// Integration tests for the CreatePet endpoint
/// Tests the complete HTTP request/response flow including database interactions
/// </summary>
public class CreatePetIntegrationTests : IntegrationTestBase
{
    public CreatePetIntegrationTests(PetHubWebApplicationFactory factory)
        : base(factory) { }

    [Fact]
    public async Task CreatePet_WithValidData_ReturnsCreated()
    {
        // Arrange
        var createDto = TestConstants.DtoBuilders.CreateValidPetDto(
            speciesId: DogSpeciesId,
            breedId: FirstBreedId,
            tagIds: new List<int> { TagIds[0], TagIds[1] }
        );

        // Act
        var response = await Client.PostAsJsonAsync(TestConstants.ApiPaths.Pets, createDto);

        // Assert
        response.ShouldBeCreated();
    }

    [Fact]
    public async Task CreatePet_WithValidData_ReturnsCreatedPet()
    {
        // Arrange
        var createDto = TestConstants.DtoBuilders.CreateValidPetDto(
            speciesId: DogSpeciesId,
            breedId: FirstBreedId,
            tagIds: new List<int> { TagIds[0] },
            description: TestConstants.Pets.ShortDescription
        );
        createDto.Gender = PetGender.Female;
        createDto.Size = PetSize.Small;
        createDto.AgeInMonths = 12;
        createDto.IsCastrated = false;

        // Act
        var response = await Client.PostAsJsonAsync(TestConstants.ApiPaths.Pets, createDto);

        // Assert
        response.ShouldBeCreated();

        var createdPet = await response.ReadApiResponseDataAsync<PetResponseDto>();
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
        var createDto = TestConstants.DtoBuilders.CreateValidPetDto(
            speciesId: DogSpeciesId,
            breedId: FirstBreedId,
            name: "Location Test Pet",
            description: "Testing location header"
        );
        createDto.Gender = PetGender.Male;
        createDto.Size = PetSize.Large;
        createDto.AgeInMonths = 36;
        createDto.TagIds = new List<int>();

        // Act
        var response = await Client.PostAsJsonAsync(TestConstants.ApiPaths.Pets, createDto);

        // Assert
        response.ShouldBeCreated();
        response.Headers.Location.Should().NotBeNull();
        response.Headers.Location!.ToString().Should().Contain("/api/Pets/");
    }

    [Fact]
    public async Task CreatePet_CreatedPetCanBeRetrieved()
    {
        // Arrange
        var createDto = TestConstants.DtoBuilders.CreateValidPetDto(
            speciesId: DogSpeciesId,
            breedId: FirstBreedId,
            tagIds: new List<int> { TagIds[0] },
            name: "Retrievable Pet",
            description: "Can be retrieved after creation"
        );
        createDto.Gender = PetGender.Unknown;
        createDto.Size = PetSize.Medium;
        createDto.AgeInMonths = 18;

        // Act
        var createResponse = await Client.PostAsJsonAsync(TestConstants.ApiPaths.Pets, createDto);
        createResponse.ShouldBeCreated();

        var createdPet = await createResponse.ReadApiResponseDataAsync<PetResponseDto>();
        var getResponse = await Client.GetAsync(TestConstants.ApiPaths.PetById(createdPet!.Id));

        // Assert
        getResponse.ShouldBeOk();
        var retrievedPet = await getResponse.ReadApiResponseDataAsync<PetResponseDto>();
        retrievedPet.Should().NotBeNull();
        retrievedPet!.Id.Should().Be(createdPet.Id);
        retrievedPet.Name.Should().Be(createDto.Name);
    }

    [Fact]
    public async Task CreatePet_WithInvalidSpeciesId_ReturnsBadRequest()
    {
        // Arrange
        var createDto = TestConstants.DtoBuilders.CreateValidPetDto(
            speciesId: 99999, // Non-existent species
            breedId: FirstBreedId,
            name: "Invalid Species Pet"
        );
        createDto.Gender = PetGender.Male;
        createDto.Size = PetSize.Medium;
        createDto.AgeInMonths = 24;

        // Act
        var response = await Client.PostAsJsonAsync(TestConstants.ApiPaths.Pets, createDto);

        // Assert
        response.ShouldBeBadRequest();
        var apiResponse = await response.ReadApiResponseAsync<object>();
        apiResponse.Should().NotBeNull();
        apiResponse!.Success.Should().BeFalse();
        apiResponse.Errors.Should().Contain(e => e.Contains("Species") && e.Contains("not found"));
    }

    [Fact]
    public async Task CreatePet_WithInvalidBreedId_ReturnsBadRequest()
    {
        // Arrange
        var createDto = TestConstants.DtoBuilders.CreateValidPetDto(
            speciesId: DogSpeciesId,
            breedId: 99999, // Non-existent breed
            name: "Invalid Breed Pet"
        );
        createDto.Gender = PetGender.Female;
        createDto.Size = PetSize.Small;
        createDto.AgeInMonths = 12;

        // Act
        var response = await Client.PostAsJsonAsync(TestConstants.ApiPaths.Pets, createDto);

        // Assert
        response.ShouldBeBadRequest();
        var apiResponse = await response.ReadApiResponseAsync<object>();
        apiResponse.Should().NotBeNull();
        apiResponse!.Success.Should().BeFalse();
        apiResponse.Errors.Should().Contain(e => e.Contains("Breed") && e.Contains("not found"));
    }

    [Fact]
    public async Task CreatePet_WithBreedFromDifferentSpecies_ReturnsBadRequest()
    {
        // Arrange
        using var scope = Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Get a breed from a different species
        var species1 = dbContext.Species.First();
        var species2 = dbContext.Species.Skip(1).First();
        var breedFromSpecies2 = dbContext.Breeds.First(b => b.SpeciesId == species2.Id);

        var createDto = TestConstants.DtoBuilders.CreateValidPetDto(
            speciesId: species1.Id,
            breedId: breedFromSpecies2.Id, // Breed from different species
            name: "Mismatched Breed Pet"
        );
        createDto.Gender = PetGender.Male;
        createDto.Size = PetSize.Medium;
        createDto.AgeInMonths = 24;

        // Act
        var response = await Client.PostAsJsonAsync(TestConstants.ApiPaths.Pets, createDto);

        // Assert
        response.ShouldBeBadRequest();
        var apiResponse = await response.ReadApiResponseAsync<object>();
        apiResponse.Should().NotBeNull();
        apiResponse!.Success.Should().BeFalse();
        apiResponse
            .Errors.Should()
            .Contain(e => e.Contains("Breed") && e.Contains("doesn't belong"));
    }

    [Fact]
    public async Task CreatePet_WithInvalidTagIds_ReturnsBadRequest()
    {
        // Arrange
        var createDto = TestConstants.DtoBuilders.CreateValidPetDto(
            speciesId: DogSpeciesId,
            breedId: FirstBreedId,
            tagIds: new List<int> { TagIds[0], 99999 }, // One valid, one invalid
            name: "Invalid Tags Pet"
        );
        createDto.Gender = PetGender.Male;
        createDto.Size = PetSize.Large;
        createDto.AgeInMonths = 36;

        // Act
        var response = await Client.PostAsJsonAsync(TestConstants.ApiPaths.Pets, createDto);

        // Assert
        response.ShouldBeBadRequest();
        var apiResponse = await response.ReadApiResponseAsync<object>();
        apiResponse.Should().NotBeNull();
        apiResponse!.Success.Should().BeFalse();
        apiResponse.Errors.Should().Contain(e => e.Contains("tag") && e.Contains("99999"));
    }

    [Fact]
    public async Task CreatePet_WithoutName_IsValid()
    {
        // Arrange - Name is optional (nullable)
        var createDto = TestConstants.DtoBuilders.CreateValidPetDto(
            speciesId: DogSpeciesId,
            breedId: FirstBreedId
        );
        createDto.Name = null; // Explicitly set to null
        createDto.Gender = PetGender.Unknown;
        createDto.AgeInMonths = 0; // Unknown age

        // Act
        var response = await Client.PostAsJsonAsync(TestConstants.ApiPaths.Pets, createDto);

        // Assert
        response.ShouldBeCreated();
        var createdPet = await response.ReadApiResponseDataAsync<PetResponseDto>();
        createdPet.Should().NotBeNull();
        createdPet!.Name.Should().BeNullOrEmpty();
    }

    [Fact]
    public async Task CreatePet_WithMultipleImages_SavesAllImages()
    {
        // Arrange
        var createDto = TestConstants.DtoBuilders.CreateValidPetDto(
            speciesId: DogSpeciesId,
            breedId: FirstBreedId,
            name: TestConstants.Pets.Max
        );
        createDto.Gender = PetGender.Female;
        createDto.Size = PetSize.Small;
        createDto.AgeInMonths = 6;
        createDto.ImageUrls = TestConstants.ImageUrls.MultipleImages();

        // Act
        var response = await Client.PostAsJsonAsync(TestConstants.ApiPaths.Pets, createDto);

        // Assert
        response.ShouldBeCreated();
        var createdPet = await response.ReadApiResponseDataAsync<PetResponseDto>();
        createdPet.Should().NotBeNull();
        createdPet!.ImageUrls.Should().HaveCount(3);
        createdPet.ImageUrls.Should().Contain(TestConstants.ImageUrls.Image1);
        createdPet.ImageUrls.Should().Contain(TestConstants.ImageUrls.Image2);
        createdPet.ImageUrls.Should().Contain(TestConstants.ImageUrls.Image3);
    }

    [Fact]
    public async Task CreatePet_WithMultipleTags_SavesAllTags()
    {
        // Arrange
        using var scope = Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var allTagIds = dbContext.Tags.Select(t => t.Id).ToList();

        var createDto = TestConstants.DtoBuilders.CreateValidPetDto(
            speciesId: DogSpeciesId,
            breedId: FirstBreedId,
            tagIds: allTagIds,
            name: TestConstants.Pets.Mia
        );
        createDto.Gender = PetGender.Male;
        createDto.Size = PetSize.Large;
        createDto.AgeInMonths = 48;

        // Act
        var response = await Client.PostAsJsonAsync(TestConstants.ApiPaths.Pets, createDto);

        // Assert
        response.ShouldBeCreated();
        var createdPet = await response.ReadApiResponseDataAsync<PetResponseDto>();
        createdPet.Should().NotBeNull();
        createdPet!.Tags.Should().HaveCount(allTagIds.Count);
    }

    [Fact]
    public async Task CreatePet_WithNoTags_IsValid()
    {
        // Arrange
        var createDto = TestConstants.DtoBuilders.CreateValidPetDto(
            speciesId: DogSpeciesId,
            breedId: FirstBreedId,
            name: "No Tags Pet"
        );
        createDto.Gender = PetGender.Female;
        createDto.Size = PetSize.Medium;
        createDto.AgeInMonths = 24;
        createDto.TagIds = new List<int>(); // No tags

        // Act
        var response = await Client.PostAsJsonAsync(TestConstants.ApiPaths.Pets, createDto);

        // Assert
        response.ShouldBeCreated();
        var createdPet = await response.ReadApiResponseDataAsync<PetResponseDto>();
        createdPet.Should().NotBeNull();
        createdPet!.Tags.Should().BeEmpty();
    }

    [Fact]
    public async Task CreatePet_DefaultsToNotAdopted()
    {
        // Arrange
        var createDto = TestConstants.DtoBuilders.CreateValidPetDto(
            speciesId: DogSpeciesId,
            breedId: FirstBreedId,
            name: "Not Adopted Pet"
        );
        createDto.Gender = PetGender.Male;
        createDto.Size = PetSize.Medium;
        createDto.AgeInMonths = 24;

        // Act
        var response = await Client.PostAsJsonAsync(TestConstants.ApiPaths.Pets, createDto);

        // Assert
        response.ShouldBeCreated();
        var createdPet = await response.ReadApiResponseDataAsync<PetResponseDto>();
        createdPet.Should().NotBeNull();
        createdPet!.IsAdopted.Should().BeFalse();
    }

    [Fact]
    public async Task CreatePet_AssignsToAuthenticatedUser()
    {
        // Arrange
        var createDto = TestConstants.DtoBuilders.CreateValidPetDto(
            speciesId: DogSpeciesId,
            breedId: FirstBreedId,
            name: "User Assignment Test"
        );
        createDto.Gender = PetGender.Female;
        createDto.Size = PetSize.Small;
        createDto.AgeInMonths = 12;

        // Act
        var response = await Client.PostAsJsonAsync(TestConstants.ApiPaths.Pets, createDto);

        // Assert
        response.ShouldBeCreated();
        var createdPet = await response.ReadApiResponseDataAsync<PetResponseDto>();
        createdPet.Should().NotBeNull();
        createdPet!.Owner.Should().NotBeNull();
        createdPet.Owner.Id.Should().NotBe(Guid.Empty);
        createdPet.Owner.Email.Should().NotBeNullOrWhiteSpace(); // The authenticated user
        createdPet.Owner.Email.Should().Contain("@example.com");
    }

    [Fact]
    public async Task CreatePet_SetsCreatedAtTimestamp()
    {
        // Arrange
        var beforeCreation = DateTime.UtcNow.AddSeconds(-5);
        var createDto = TestConstants.DtoBuilders.CreateValidPetDto(
            speciesId: DogSpeciesId,
            breedId: FirstBreedId,
            name: "Timestamp Test Pet"
        );
        createDto.Gender = PetGender.Female;
        createDto.Size = PetSize.Medium;
        createDto.AgeInMonths = 18;

        // Act
        var response = await Client.PostAsJsonAsync(TestConstants.ApiPaths.Pets, createDto);
        var afterCreation = DateTime.UtcNow.AddSeconds(5);

        // Assert
        response.ShouldBeCreated();
        var createdPet = await response.ReadApiResponseDataAsync<PetResponseDto>();
        createdPet.Should().NotBeNull();
        createdPet!.CreatedAt.Should().BeAfter(beforeCreation);
        createdPet.CreatedAt.Should().BeBefore(afterCreation);
    }

    [Fact]
    public async Task CreatePet_WithZeroAge_IsValid()
    {
        // Arrange - Age 0 means unknown/estimated
        var createDto = TestConstants.DtoBuilders.CreateValidPetDto(
            speciesId: DogSpeciesId,
            breedId: FirstBreedId,
            name: "Unknown Age Pet"
        );
        createDto.Gender = PetGender.Unknown;
        createDto.Size = PetSize.Medium;
        createDto.AgeInMonths = 0; // Unknown age

        // Act
        var response = await Client.PostAsJsonAsync(TestConstants.ApiPaths.Pets, createDto);

        // Assert
        response.ShouldBeCreated();
        var createdPet = await response.ReadApiResponseDataAsync<PetResponseDto>();
        createdPet.Should().NotBeNull();
        createdPet!.AgeInMonths.Should().Be(0);
    }

    [Fact]
    public async Task CreatePet_IncludesAllRelationshipsInResponse()
    {
        // Arrange
        var createDto = TestConstants.DtoBuilders.CreateValidPetDto(
            speciesId: DogSpeciesId,
            breedId: FirstBreedId,
            tagIds: new List<int> { TagIds[0], TagIds[1] },
            name: "Full Relationships Pet",
            description: "Testing all relationships"
        );
        createDto.Gender = PetGender.Male;
        createDto.Size = PetSize.Large;
        createDto.AgeInMonths = 36;
        createDto.IsCastrated = true;
        createDto.IsVaccinated = true;

        // Act
        var response = await Client.PostAsJsonAsync(TestConstants.ApiPaths.Pets, createDto);

        // Assert
        response.ShouldBeCreated();
        var createdPet = await response.ReadApiResponseDataAsync<PetResponseDto>();
        createdPet.Should().NotBeNull();

        // Verify all relationships are loaded
        createdPet!.Owner.Should().NotBeNull();
        createdPet.Owner.Id.Should().NotBe(Guid.Empty);

        createdPet.SpeciesName.Should().NotBeNullOrEmpty();
        createdPet.BreedName.Should().NotBeNullOrEmpty();

        createdPet.ImageUrls.Should().NotBeEmpty();
        createdPet.ImageUrls.Should().HaveCount(1);

        createdPet.Tags.Should().NotBeEmpty();
        createdPet.Tags.Should().HaveCount(2);
        createdPet.Tags.Should().OnlyContain(t => !string.IsNullOrEmpty(t.Name));
    }

    [Fact]
    public async Task CreatePet_WithoutAuthentication_ReturnsUnauthorized()
    {
        // Arrange
        var clientWithoutAuth = Factory.CreateClient(); // New client without token
        var createDto = TestConstants.DtoBuilders.CreateValidPetDto(
            speciesId: DogSpeciesId,
            breedId: FirstBreedId,
            tagIds: new List<int> { TagIds[0] },
            name: "Unauthorized Test",
            description: "Should fail without auth"
        );
        createDto.Gender = PetGender.Male;
        createDto.Size = PetSize.Medium;
        createDto.AgeInMonths = 12;

        // Act
        var response = await clientWithoutAuth.PostAsJsonAsync(
            TestConstants.ApiPaths.Pets,
            createDto
        );

        // Assert
        response.ShouldBeUnauthorized();
    }
}
