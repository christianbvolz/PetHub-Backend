using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using PetHub.API.Data;
using PetHub.API.DTOs.Common;
using PetHub.API.DTOs.Pet;
using PetHub.API.Enums;
using PetHub.Tests.IntegrationTests.Helpers;
using PetHub.Tests.IntegrationTests.Infrastructure;

namespace PetHub.Tests.IntegrationTests.Controllers.PetsControllerTests;

/// <summary>
/// Integration tests for the GetMyPets endpoint (GET /api/pets/me)
/// Tests retrieving authenticated user's pets
/// </summary>
public class GetMyPetsTests : IClassFixture<PetHubWebApplicationFactory>, IAsyncLifetime
{
    private readonly HttpClient _client;
    private readonly PetHubWebApplicationFactory _factory;
    private string _userToken = string.Empty;
    private string _otherUserToken = string.Empty;
    private readonly List<int> _userPetIds = new();

    public GetMyPetsTests(PetHubWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    public async Task InitializeAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await dbContext.Database.EnsureDeletedAsync();
        await dbContext.Database.EnsureCreatedAsync();
        await TestDataSeeder.SeedTestData(dbContext);

        // Register user and create multiple test pets
        _userToken = await AuthenticationHelper.RegisterAndGetTokenAsync(
            _client,
            "mypetsuser@example.com"
        );
        _client.AddAuthToken(_userToken);

        var species = dbContext.Species.First();
        var breed = dbContext.Breeds.First(b => b.SpeciesId == species.Id);

        // Create 3 pets for the user
        for (int petIndex = 1; petIndex <= 3; petIndex++)
        {
            var createDto = new CreatePetDto
            {
                Name = $"My Pet {petIndex}",
                SpeciesId = species.Id,
                BreedId = breed.Id,
                Gender = PetGender.Male,
                Size = PetSize.Medium,
                AgeInMonths = 12 * petIndex,
                ImageUrls = new List<string> { $"https://example.com/pet{petIndex}.jpg" },
            };

            var response = await _client.PostAsJsonAsync("/api/pets", createDto);
            var createdPet = await response.ReadApiResponseDataAsync<PetResponseDto>();
            _userPetIds.Add(createdPet!.Id);
        }

        // Register another user and create a pet for them
        _otherUserToken = await AuthenticationHelper.RegisterAndGetTokenAsync(
            _client,
            "otherpetsuser@example.com"
        );
        _client.AddAuthToken(_otherUserToken);

        var otherPetDto = new CreatePetDto
        {
            Name = "Other User Pet",
            SpeciesId = species.Id,
            BreedId = breed.Id,
            Gender = PetGender.Female,
            Size = PetSize.Small,
            AgeInMonths = 6,
            ImageUrls = new List<string> { "https://example.com/other.jpg" },
        };

        await _client.PostAsJsonAsync("/api/pets", otherPetDto);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task GetMyPets_ReturnsOk()
    {
        // Arrange
        _client.AddAuthToken(_userToken);

        // Act
        var response = await _client.GetAsync("/api/pets/me");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetMyPets_ReturnsOnlyUsersPets()
    {
        // Arrange
        _client.AddAuthToken(_userToken);

        // Act
        var response = await _client.GetAsync("/api/pets/me");

        // Assert
        var pets = await response.ReadApiResponseDataAsync<List<PetResponseDto>>();
        pets.Should().NotBeNull();
        pets!.Should().HaveCount(3);
        pets.Should().OnlyContain(p => _userPetIds.Contains(p.Id));
    }

    [Fact]
    public async Task GetMyPets_ReturnsEmptyListWhenUserHasNoPets()
    {
        // Arrange - Register a new user without pets
        var newUserToken = await AuthenticationHelper.RegisterAndGetTokenAsync(
            _client,
            "nopetsuser@example.com"
        );
        _client.AddAuthToken(newUserToken);

        // Act
        var response = await _client.GetAsync("/api/pets/me");

        // Assert
        var pets = await response.ReadApiResponseDataAsync<List<PetResponseDto>>();
        pets.Should().NotBeNull();
        pets!.Should().BeEmpty();
    }

    [Fact]
    public async Task GetMyPets_DoesNotReturnOtherUsersPets()
    {
        // Arrange
        _client.AddAuthToken(_userToken);

        // Act
        var response = await _client.GetAsync("/api/pets/me");

        // Assert
        var pets = await response.ReadApiResponseDataAsync<List<PetResponseDto>>();
        pets.Should().NotBeNull();
        pets!.Should().NotContain(p => p.Name == "Other User Pet");
    }

    [Fact]
    public async Task GetMyPets_WithoutAuthentication_ReturnsUnauthorized()
    {
        // Arrange
        var clientWithoutAuth = _factory.CreateClient();

        // Act
        var response = await clientWithoutAuth.GetAsync("/api/pets/me");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetMyPets_ReturnsCompleteData()
    {
        // Arrange
        _client.AddAuthToken(_userToken);

        // Act
        var response = await _client.GetAsync("/api/pets/me");

        // Assert
        var pets = await response.ReadApiResponseDataAsync<List<PetResponseDto>>();
        pets.Should().NotBeNull();
        pets!.Should().HaveCount(3);

        var firstPet = pets!.First();
        firstPet.Id.Should().BeGreaterThan(0);
        firstPet.Name.Should().NotBeNullOrEmpty();
        firstPet.Owner.Should().NotBeNull();
        firstPet.SpeciesName.Should().NotBeNullOrEmpty();
        firstPet.BreedName.Should().NotBeNullOrEmpty();
        firstPet.ImageUrls.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetMyPets_ReturnsPetsOrderedByCreatedAt()
    {
        // Arrange
        _client.AddAuthToken(_userToken);

        // Act
        var response = await _client.GetAsync("/api/pets/me");

        // Assert
        var pets = await response.ReadApiResponseDataAsync<List<PetResponseDto>>();
        pets.Should().NotBeNull();
        pets = pets!;
        pets.Should().HaveCount(3);

        // Verify descending order (newest first)
        for (int index = 0; index < pets.Count - 1; index++)
        {
            pets[index].CreatedAt.Should().BeOnOrAfter(pets[index + 1].CreatedAt);
        }
    }

    [Fact]
    public async Task GetMyPets_IncludesAdoptedPets()
    {
        // Arrange
        _client.AddAuthToken(_userToken);

        // Mark one pet as adopted
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var pet = await dbContext.Pets.FindAsync(_userPetIds[0]);
        pet!.IsAdopted = true;
        await dbContext.SaveChangesAsync();

        // Act
        var response = await _client.GetAsync("/api/pets/me");

        // Assert
        var pets = await response.ReadApiResponseDataAsync<List<PetResponseDto>>();
        pets.Should().NotBeNull();
        pets!.Should().HaveCount(3); // All pets including adopted
        pets.Should().Contain(p => p.IsAdopted);
    }

    [Fact]
    public async Task GetMyPets_AfterDeletingPet_ReturnsCorrectCount()
    {
        // Arrange
        _client.AddAuthToken(_userToken);

        // Delete one pet
        await _client.DeleteAsync($"/api/pets/{_userPetIds[0]}");

        // Act
        var response = await _client.GetAsync("/api/pets/me");

        // Assert
        var pets = await response.ReadApiResponseDataAsync<List<PetResponseDto>>();
        pets.Should().NotBeNull();
        pets!.Should().HaveCount(2);
        pets.Should().NotContain(p => p.Id == _userPetIds[0]);
    }

    [Fact]
    public async Task GetMyPets_DifferentUsers_GetDifferentPets()
    {
        // Arrange & Act
        _client.AddAuthToken(_userToken);
        var userResponse = await _client.GetAsync("/api/pets/me");
        var userPets = await userResponse.ReadApiResponseDataAsync<List<PetResponseDto>>();

        _client.AddAuthToken(_otherUserToken);
        var otherResponse = await _client.GetAsync("/api/pets/me");
        var otherPets = await otherResponse.ReadApiResponseDataAsync<List<PetResponseDto>>();

        // Assert
        userPets!.Should().HaveCount(3);
        otherPets!.Should().HaveCount(1);
        userPets.Should().NotContain(p => p.Name == "Other User Pet");
        otherPets.Should().Contain(p => p.Name == "Other User Pet");
    }

    [Fact]
    public async Task GetMyPets_WithApiResponseWrapper()
    {
        // Arrange
        _client.AddAuthToken(_userToken);

        // Act
        var response = await _client.GetAsync("/api/pets/me");

        // Assert
        var apiResponse = await response.ReadApiResponseAsync<List<PetResponseDto>>();
        apiResponse.Should().NotBeNull();
        apiResponse!.Success.Should().BeTrue();
        apiResponse.Data.Should().NotBeNull();
        apiResponse.Data!.Should().HaveCount(3);
    }
}
