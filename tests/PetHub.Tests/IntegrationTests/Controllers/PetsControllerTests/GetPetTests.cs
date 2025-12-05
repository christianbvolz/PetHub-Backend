using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using PetHub.API.Data;
using PetHub.API.DTOs.Pet;
using PetHub.API.Enums;
using PetHub.Tests;
using PetHub.Tests.Extensions;
using PetHub.Tests.IntegrationTests.Helpers;
using PetHub.Tests.IntegrationTests.Infrastructure;

namespace PetHub.Tests.IntegrationTests.Controllers.PetsControllerTests;

/// <summary>
/// Integration tests for the GetPet endpoint
/// Tests the complete HTTP request/response flow including database interactions
/// </summary>
public class GetPetIntegrationTests : IntegrationTestBase
{
    private int _existingPetId;

    public GetPetIntegrationTests(PetHubWebApplicationFactory factory)
        : base(factory) { }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();

        // Get an existing pet ID for tests
        await WithDbContextAsync(async dbContext =>
        {
            var existingPet = await dbContext.Pets.FirstOrDefaultAsync();
            _existingPetId = existingPet?.Id ?? 0;
        });
    }

    [Fact]
    public async Task GetPet_WithValidId_ReturnsOk()
    {
        // Arrange
        var requestUri = TestConstants.ApiPaths.PetById(_existingPetId);

        // Act
        var response = await Client.GetAsync(requestUri);

        // Assert
        response.ShouldBeOk();
    }

    [Fact]
    public async Task GetPet_WithValidId_ReturnsPetDetails()
    {
        // Arrange
        var requestUri = TestConstants.ApiPaths.PetById(_existingPetId);

        // Act
        var response = await Client.GetAsync(requestUri);

        // Assert
        response.ShouldBeOk();

        var pet = await response.ReadApiResponseDataAsync<PetResponseDto>();
        pet.Should().NotBeNull();
        pet!.Id.Should().Be(_existingPetId);
        pet.Name.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetPet_WithValidId_IncludesAllRelationships()
    {
        // Arrange
        var requestUri = TestConstants.ApiPaths.PetById(_existingPetId);

        // Act
        var response = await Client.GetAsync(requestUri);

        // Assert
        response.ShouldBeOk();

        var pet = await response.ReadApiResponseDataAsync<PetResponseDto>();
        pet.Should().NotBeNull();

        // Check all required relationships are loaded
        pet!.Id.Should().BeGreaterThan(0);
        pet.Name.Should().NotBeNullOrEmpty();
        pet.SpeciesName.Should().NotBeNullOrEmpty();
        pet.BreedName.Should().NotBeNullOrEmpty();
        pet.Gender.Should().BeDefined();
        pet.Size.Should().BeDefined();
        pet.AgeInMonths.Should().BeGreaterThan(0);

        // Owner relationship
        pet.Owner.Should().NotBeNull();
        pet.Owner.Id.Should().NotBe(Guid.Empty);
        pet.Owner.Name.Should().NotBeNullOrEmpty();
        pet.Owner.City.Should().NotBeNullOrEmpty();
        pet.Owner.State.Should().NotBeNullOrEmpty();

        // Tags relationship
        pet.Tags.Should().NotBeNull();
    }

    [Fact]
    public async Task GetPet_WithNonExistentId_ReturnsNotFound()
    {
        // Arrange
        var nonExistentId = 999999;
        var requestUri = TestConstants.ApiPaths.PetById(nonExistentId);

        // Act
        var response = await Client.GetAsync(requestUri);

        // Assert
        response.ShouldBeNotFound();
    }

    [Fact]
    public async Task GetPet_WithInvalidId_ReturnsBadRequest()
    {
        // Arrange
        var requestUri = $"{TestConstants.ApiPaths.Pets}/invalid";

        // Act
        var response = await Client.GetAsync(requestUri);

        // Assert
        response.ShouldBeBadRequest();
    }

    [Fact]
    public async Task GetPet_WithZeroId_ReturnsNotFound()
    {
        // Arrange
        var requestUri = TestConstants.ApiPaths.PetById(0);

        // Act
        var response = await Client.GetAsync(requestUri);

        // Assert
        response.ShouldBeNotFound();
    }

    [Fact]
    public async Task GetPet_WithNegativeId_ReturnsNotFound()
    {
        // Arrange
        var requestUri = TestConstants.ApiPaths.PetById(-1);

        // Act
        var response = await Client.GetAsync(requestUri);

        // Assert
        response.ShouldBeNotFound();
    }

    [Fact]
    public async Task GetPet_ResponseStructure_IsCorrect()
    {
        // Arrange
        var requestUri = TestConstants.ApiPaths.PetById(_existingPetId);

        // Act
        var response = await Client.GetAsync(requestUri);

        // Assert
        response.ShouldBeOk();

        var pet = await response.ReadApiResponseDataAsync<PetResponseDto>();
        pet.Should().NotBeNull();

        // Verify response structure
        pet!.Id.Should().BeGreaterThan(0);
        pet.Name.Should().NotBeNullOrEmpty();
        pet.SpeciesName.Should().NotBeNullOrEmpty();
        pet.BreedName.Should().NotBeNullOrEmpty();
        pet.Gender.Should()
            .BeDefined()
            .And.BeOneOf(PetGender.Male, PetGender.Female, PetGender.Unknown);
        pet.Size.Should().BeDefined().And.BeOneOf(PetSize.Small, PetSize.Medium, PetSize.Large);
        pet.AgeInMonths.Should().BeGreaterThan(0);
        pet.Description.Should().NotBeNullOrEmpty();

        // Boolean fields are value types (non-nullable) and are guaranteed to be true or false
        pet.IsAdopted.Should().Be(false); // Replace with expected value from test seeding

        // Owner structure
        pet.Owner.Should().NotBeNull();
        pet.Owner.Id.Should().NotBe(Guid.Empty);
        pet.Owner.Name.Should().NotBeNullOrEmpty();
        pet.Owner.City.Should().NotBeNullOrEmpty();
        pet.Owner.State.Should().NotBeNullOrEmpty();

        // ImageUrls and Tags (can be empty but should be initialized)
        pet.ImageUrls.Should().NotBeNull();
        pet.Tags.Should().NotBeNull();
    }

    [Fact]
    public async Task GetPet_WithTags_ReturnsTagsCorrectly()
    {
        // Arrange - Get "Rex" who has tags
        using var scope = Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var rexId =
            (await dbContext.Pets.FirstOrDefaultAsync(p => p.Name == TestConstants.Pets.Rex))?.Id
            ?? 0;

        var requestUri = TestConstants.ApiPaths.PetById(rexId);

        // Act
        var response = await Client.GetAsync(requestUri);

        // Assert
        response.ShouldBeOk();

        var pet = await response.ReadApiResponseDataAsync<PetResponseDto>();
        pet.Should().NotBeNull();
        pet!.Tags.Should().NotBeNullOrEmpty();
        pet.Tags.Should().Contain(t => t.Name == "Brown");
        pet.Tags.Should().Contain(t => t.Name == "Short Coat");
        pet.Tags.Should()
            .OnlyContain(t => t.Category == TagCategory.Color || t.Category == TagCategory.Coat);
    }

    [Fact]
    public async Task GetPet_IncludesAdoptedPets_WhenQuerying()
    {
        // Arrange - Get "Adopted Pet" from seeded data
        using var scope = Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var adoptedPetId =
            (await dbContext.Pets.FirstOrDefaultAsync(p => p.Name == TestConstants.Pets.Thor))?.Id
            ?? 0;

        var requestUri = TestConstants.ApiPaths.PetById(adoptedPetId);

        // Act
        var response = await Client.GetAsync(requestUri);

        // Assert
        response.ShouldBeOk();

        var pet = await response.ReadApiResponseDataAsync<PetResponseDto>();
        pet.Should().NotBeNull();
        pet!.Name.Should().Be(TestConstants.Pets.Thor);
        pet.IsAdopted.Should().BeTrue();
    }

    [Fact]
    public async Task GetPet_ReturnsConsistentData_OnMultipleRequests()
    {
        // Arrange
        var requestUri = TestConstants.ApiPaths.PetById(_existingPetId);

        // Act
        var response1 = await Client.GetAsync(requestUri);
        var pet1 = await response1.ReadApiResponseDataAsync<PetResponseDto>();

        var response2 = await Client.GetAsync(requestUri);
        var pet2 = await response2.ReadApiResponseDataAsync<PetResponseDto>();

        // Assert
        response1.ShouldBeOk();
        response2.ShouldBeOk();

        pet1.Should().NotBeNull();
        pet2.Should().NotBeNull();

        // Verify both responses return the same data
        pet1!.Id.Should().Be(pet2!.Id);
        pet1.Name.Should().Be(pet2.Name);
        pet1.SpeciesName.Should().Be(pet2.SpeciesName);
        pet1.BreedName.Should().Be(pet2.BreedName);
        pet1.Gender.Should().Be(pet2.Gender);
        pet1.Size.Should().Be(pet2.Size);
        pet1.AgeInMonths.Should().Be(pet2.AgeInMonths);
    }
}
