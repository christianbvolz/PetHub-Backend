using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using PetHub.API.Data;
using PetHub.API.DTOs.Pet;
using PetHub.API.Enums;
using PetHub.Tests.IntegrationTests.Helpers;
using PetHub.Tests.IntegrationTests.Infrastructure;

namespace PetHub.Tests.IntegrationTests.Controllers.PetsControllerTests;

/// <summary>
/// Integration tests for the GetPet endpoint
/// Tests the complete HTTP request/response flow including database interactions
/// </summary>
public class GetPetIntegrationTests : IClassFixture<PetHubWebApplicationFactory>, IAsyncLifetime
{
    private readonly HttpClient _client;
    private readonly PetHubWebApplicationFactory _factory;
    private int _existingPetId;

    public GetPetIntegrationTests(PetHubWebApplicationFactory factory)
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

        // Get an existing pet ID for tests
        var existingPet = dbContext.Pets.FirstOrDefault();
        _existingPetId = existingPet?.Id ?? 0;
    }

    public Task DisposeAsync()
    {
        // Cleanup if needed
        return Task.CompletedTask;
    }

    [Fact]
    public async Task GetPet_WithValidId_ReturnsOk()
    {
        // Arrange
        var requestUri = $"/api/pets/{_existingPetId}";

        // Act
        var response = await _client.GetAsync(requestUri);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetPet_WithValidId_ReturnsPetDetails()
    {
        // Arrange
        var requestUri = $"/api/pets/{_existingPetId}";

        // Act
        var response = await _client.GetAsync(requestUri);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var pet = await response.ReadApiResponseDataAsync<PetResponseDto>();
        pet.Should().NotBeNull();
        pet!.Id.Should().Be(_existingPetId);
        pet.Name.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetPet_WithValidId_IncludesAllRelationships()
    {
        // Arrange
        var requestUri = $"/api/pets/{_existingPetId}";

        // Act
        var response = await _client.GetAsync(requestUri);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

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
        var requestUri = $"/api/pets/{nonExistentId}";

        // Act
        var response = await _client.GetAsync(requestUri);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetPet_WithInvalidId_ReturnsBadRequest()
    {
        // Arrange
        var requestUri = "/api/pets/invalid";

        // Act
        var response = await _client.GetAsync(requestUri);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetPet_WithZeroId_ReturnsNotFound()
    {
        // Arrange
        var requestUri = "/api/pets/0";

        // Act
        var response = await _client.GetAsync(requestUri);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetPet_WithNegativeId_ReturnsNotFound()
    {
        // Arrange
        var requestUri = "/api/pets/-1";

        // Act
        var response = await _client.GetAsync(requestUri);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetPet_ResponseStructure_IsCorrect()
    {
        // Arrange
        var requestUri = $"/api/pets/{_existingPetId}";

        // Act
        var response = await _client.GetAsync(requestUri);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

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
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var rexId = dbContext.Pets.FirstOrDefault(p => p.Name == "Rex")?.Id ?? 0;

        var requestUri = $"/api/pets/{rexId}";

        // Act
        var response = await _client.GetAsync(requestUri);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var pet = await response.ReadApiResponseDataAsync<PetResponseDto>();
        pet.Should().NotBeNull();
        pet!.Tags.Should().NotBeNullOrEmpty();
        pet.Tags.Should().Contain(t => t.Name == "Marrom");
        pet.Tags.Should().Contain(t => t.Name == "Curto");
        pet.Tags.Should()
            .OnlyContain(t => t.Category == TagCategory.Color || t.Category == TagCategory.Coat);
    }

    [Fact]
    public async Task GetPet_IncludesAdoptedPets_WhenQuerying()
    {
        // Arrange - Get "Adopted Pet"
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var adoptedPetId = dbContext.Pets.FirstOrDefault(p => p.Name == "Adopted Pet")?.Id ?? 0;

        var requestUri = $"/api/pets/{adoptedPetId}";

        // Act
        var response = await _client.GetAsync(requestUri);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var pet = await response.ReadApiResponseDataAsync<PetResponseDto>();
        pet.Should().NotBeNull();
        pet!.Name.Should().Be("Adopted Pet");
        pet.IsAdopted.Should().BeTrue();
    }

    [Fact]
    public async Task GetPet_ReturnsConsistentData_OnMultipleRequests()
    {
        // Arrange
        var requestUri = $"/api/pets/{_existingPetId}";

        // Act
        var response1 = await _client.GetAsync(requestUri);
        var pet1 = await response1.ReadApiResponseDataAsync<PetResponseDto>();

        var response2 = await _client.GetAsync(requestUri);
        var pet2 = await response2.ReadApiResponseDataAsync<PetResponseDto>();

        // Assert
        response1.StatusCode.Should().Be(HttpStatusCode.OK);
        response2.StatusCode.Should().Be(HttpStatusCode.OK);

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
