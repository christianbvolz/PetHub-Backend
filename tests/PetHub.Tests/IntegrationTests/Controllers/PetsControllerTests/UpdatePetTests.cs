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
/// Integration tests for the UpdatePet endpoint (PATCH /api/pets/{id})
/// Tests ownership validation, partial updates, and data validation
/// </summary>
public class UpdatePetTests : IClassFixture<PetHubWebApplicationFactory>, IAsyncLifetime
{
    private readonly HttpClient _client;
    private readonly PetHubWebApplicationFactory _factory;
    private string _ownerToken = string.Empty;
    private string _otherUserToken = string.Empty;
    private int _testPetId;
    private int _validBreedId;
    private int _validTagId;

    public UpdatePetTests(PetHubWebApplicationFactory factory)
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

        // Register owner and create a test pet
        _ownerToken = await AuthenticationHelper.RegisterAndGetTokenAsync(
            _client,
            "owner@example.com"
        );
        _client.AddAuthToken(_ownerToken);

        var species = dbContext.Species.First();
        var breed = dbContext.Breeds.First(b => b.SpeciesId == species.Id);
        var tag = dbContext.Tags.First();

        _validBreedId = breed.Id;
        _validTagId = tag.Id;

        var createDto = new CreatePetDto
        {
            Name = "Original Pet",
            SpeciesId = species.Id,
            BreedId = breed.Id,
            Gender = PetGender.Male,
            Size = PetSize.Medium,
            AgeInMonths = 24,
            Description = "Original description",
            IsCastrated = false,
            IsVaccinated = false,
            ImageUrls = new List<string> { "https://example.com/original.jpg" },
            TagIds = new List<int> { tag.Id },
        };

        var response = await _client.PostAsJsonAsync("/api/pets", createDto);
        var createdPet = await response.ReadApiResponseDataAsync<PetResponseDto>();
        _testPetId = createdPet!.Id;

        // Register another user for ownership tests
        _otherUserToken = await AuthenticationHelper.RegisterAndGetTokenAsync(
            _client,
            "otheruser@example.com"
        );
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task UpdatePet_WithValidData_ReturnsOk()
    {
        // Arrange
        _client.AddAuthToken(_ownerToken);
        var updateDto = new UpdatePetDto
        {
            Name = "Updated Pet Name",
            Description = "Updated description",
        };

        // Act
        var response = await _client.PatchAsJsonAsync($"/api/pets/{_testPetId}", updateDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task UpdatePet_UpdatesOnlyProvidedFields()
    {
        // Arrange
        _client.AddAuthToken(_ownerToken);
        var updateDto = new UpdatePetDto { Name = "New Name Only" };

        // Act
        var response = await _client.PatchAsJsonAsync($"/api/pets/{_testPetId}", updateDto);

        // Assert
        var updatedPet = await response.ReadApiResponseDataAsync<PetResponseDto>();
        updatedPet.Should().NotBeNull();
        updatedPet!.Name.Should().Be("New Name Only");
        updatedPet.Description.Should().Be("Original description"); // Unchanged
        updatedPet.Gender.Should().Be(PetGender.Male); // Unchanged
    }

    [Fact]
    public async Task UpdatePet_UpdatesMultipleFields()
    {
        // Arrange
        _client.AddAuthToken(_ownerToken);
        var updateDto = new UpdatePetDto
        {
            Name = "Completely Updated",
            Description = "New description",
            Gender = PetGender.Female,
            Size = PetSize.Large,
            AgeInMonths = 36,
            IsCastrated = true,
            IsVaccinated = true,
        };

        // Act
        var response = await _client.PatchAsJsonAsync($"/api/pets/{_testPetId}", updateDto);

        // Assert
        var updatedPet = await response.ReadApiResponseDataAsync<PetResponseDto>();
        updatedPet.Should().NotBeNull();
        updatedPet!.Name.Should().Be("Completely Updated");
        updatedPet.Description.Should().Be("New description");
        updatedPet.Gender.Should().Be(PetGender.Female);
        updatedPet.Size.Should().Be(PetSize.Large);
        updatedPet.AgeInMonths.Should().Be(36);
        updatedPet.IsCastrated.Should().BeTrue();
        updatedPet.IsVaccinated.Should().BeTrue();
    }

    [Fact]
    public async Task UpdatePet_CanUpdateBreed()
    {
        // Arrange
        _client.AddAuthToken(_ownerToken);
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var pet = await dbContext.Pets.FindAsync(_testPetId);
        var anotherBreed = dbContext
            .Breeds.Where(b => b.SpeciesId == pet!.SpeciesId && b.Id != pet.BreedId)
            .First();

        var updateDto = new UpdatePetDto { BreedId = anotherBreed.Id };

        // Act
        var response = await _client.PatchAsJsonAsync($"/api/pets/{_testPetId}", updateDto);

        // Assert
        var updatedPet = await response.ReadApiResponseDataAsync<PetResponseDto>();
        updatedPet.Should().NotBeNull();
        updatedPet!.BreedName.Should().Be(anotherBreed.Name);
    }

    [Fact]
    public async Task UpdatePet_WithInvalidBreed_ReturnsBadRequest()
    {
        // Arrange
        _client.AddAuthToken(_ownerToken);
        var updateDto = new UpdatePetDto
        {
            BreedId = 99999, // Non-existent breed
        };

        // Act
        var response = await _client.PatchAsJsonAsync($"/api/pets/{_testPetId}", updateDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var apiResponse = await response.ReadApiResponseAsync<object>();
        apiResponse!.Errors.Should().Contain(e => e.Contains("Breed") && e.Contains("not found"));
    }

    [Fact]
    public async Task UpdatePet_WithBreedFromDifferentSpecies_ReturnsBadRequest()
    {
        // Arrange
        _client.AddAuthToken(_ownerToken);
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var pet = await dbContext.Pets.FindAsync(_testPetId);
        var differentSpeciesBreed = dbContext.Breeds.First(b => b.SpeciesId != pet!.SpeciesId);

        var updateDto = new UpdatePetDto { BreedId = differentSpeciesBreed.Id };

        // Act
        var response = await _client.PatchAsJsonAsync($"/api/pets/{_testPetId}", updateDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var apiResponse = await response.ReadApiResponseAsync<object>();
        apiResponse!.Errors.Should().Contain(e => e.Contains("doesn't belong"));
    }

    [Fact]
    public async Task UpdatePet_CanUpdateTags()
    {
        // Arrange
        _client.AddAuthToken(_ownerToken);
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var newTags = dbContext.Tags.Take(3).Select(t => t.Id).ToList();
        var updateDto = new UpdatePetDto { TagIds = newTags };

        // Act
        var response = await _client.PatchAsJsonAsync($"/api/pets/{_testPetId}", updateDto);

        // Assert
        var updatedPet = await response.ReadApiResponseDataAsync<PetResponseDto>();
        updatedPet.Should().NotBeNull();
        updatedPet!.Tags.Should().HaveCount(3);
    }

    [Fact]
    public async Task UpdatePet_WithInvalidTags_ReturnsBadRequest()
    {
        // Arrange
        _client.AddAuthToken(_ownerToken);
        var updateDto = new UpdatePetDto
        {
            TagIds = new List<int> { _validTagId, 99999 },
        };

        // Act
        var response = await _client.PatchAsJsonAsync($"/api/pets/{_testPetId}", updateDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var apiResponse = await response.ReadApiResponseAsync<object>();
        apiResponse!.Errors.Should().Contain(e => e.Contains("tag"));
    }

    [Fact]
    public async Task UpdatePet_CanUpdateImages()
    {
        // Arrange
        _client.AddAuthToken(_ownerToken);
        var updateDto = new UpdatePetDto
        {
            ImageUrls = new List<string>
            {
                "https://example.com/new1.jpg",
                "https://example.com/new2.jpg",
            },
        };

        // Act
        var response = await _client.PatchAsJsonAsync($"/api/pets/{_testPetId}", updateDto);

        // Assert
        var updatedPet = await response.ReadApiResponseDataAsync<PetResponseDto>();
        updatedPet.Should().NotBeNull();
        updatedPet!.ImageUrls.Should().HaveCount(2);
        updatedPet.ImageUrls.Should().Contain("https://example.com/new1.jpg");
    }

    [Fact]
    public async Task UpdatePet_WithNonExistentPet_ReturnsNotFound()
    {
        // Arrange
        _client.AddAuthToken(_ownerToken);
        var updateDto = new UpdatePetDto { Name = "Updated" };

        // Act
        var response = await _client.PatchAsJsonAsync("/api/pets/99999", updateDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdatePet_WithoutOwnership_ReturnsForbidden()
    {
        // Arrange
        _client.AddAuthToken(_otherUserToken); // Different user
        var updateDto = new UpdatePetDto { Name = "Trying to update" };

        // Act
        var response = await _client.PatchAsJsonAsync($"/api/pets/{_testPetId}", updateDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        var apiResponse = await response.ReadApiResponseAsync<object>();
        apiResponse!.Errors.Should().Contain(e => e.Contains("permission"));
    }

    [Fact]
    public async Task UpdatePet_WithoutAuthentication_ReturnsUnauthorized()
    {
        // Arrange
        var clientWithoutAuth = _factory.CreateClient();
        var updateDto = new UpdatePetDto { Name = "Should fail" };

        // Act
        var response = await clientWithoutAuth.PatchAsJsonAsync(
            $"/api/pets/{_testPetId}",
            updateDto
        );

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UpdatePet_WithNullName_DoesNotUpdateName()
    {
        // Arrange
        _client.AddAuthToken(_ownerToken);
        var updateDto = new UpdatePetDto
        {
            Name = null, // Not updating name
        };

        // Act
        var response = await _client.PatchAsJsonAsync($"/api/pets/{_testPetId}", updateDto);

        // Assert
        var updatedPet = await response.ReadApiResponseDataAsync<PetResponseDto>();
        updatedPet.Should().NotBeNull();
        updatedPet!.Name.Should().Be("Original Pet"); // Name unchanged
    }

    [Fact]
    public async Task UpdatePet_WithNullDescription_DoesNotUpdateDescription()
    {
        // Arrange
        _client.AddAuthToken(_ownerToken);
        var updateDto = new UpdatePetDto { Description = null };

        // Act
        var response = await _client.PatchAsJsonAsync($"/api/pets/{_testPetId}", updateDto);

        // Assert
        var updatedPet = await response.ReadApiResponseDataAsync<PetResponseDto>();
        updatedPet.Should().NotBeNull();
        updatedPet!.Description.Should().Be("Original description"); // Description unchanged
    }

    [Fact]
    public async Task UpdatePet_ReturnsAllRelationships()
    {
        // Arrange
        _client.AddAuthToken(_ownerToken);
        var updateDto = new UpdatePetDto { Name = "Check Relationships" };

        // Act
        var response = await _client.PatchAsJsonAsync($"/api/pets/{_testPetId}", updateDto);

        // Assert
        var updatedPet = await response.ReadApiResponseDataAsync<PetResponseDto>();
        updatedPet.Should().NotBeNull();
        updatedPet!.Owner.Should().NotBeNull();
        updatedPet.SpeciesName.Should().NotBeNullOrEmpty();
        updatedPet.BreedName.Should().NotBeNullOrEmpty();
        updatedPet.ImageUrls.Should().NotBeNull();
        updatedPet.Tags.Should().NotBeNull();
    }

    [Fact]
    public async Task UpdatePet_DoesNotChangeIsAdopted()
    {
        // Arrange
        _client.AddAuthToken(_ownerToken);

        // Get original IsAdopted status
        var getResponse = await _client.GetAsync($"/api/pets/{_testPetId}");
        var originalPet = await getResponse.ReadApiResponseDataAsync<PetResponseDto>();
        var originalIsAdopted = originalPet!.IsAdopted;

        var updateDto = new UpdatePetDto { Name = "Updated Name" };

        // Act
        var response = await _client.PatchAsJsonAsync($"/api/pets/{_testPetId}", updateDto);

        // Assert
        var updatedPet = await response.ReadApiResponseDataAsync<PetResponseDto>();
        updatedPet!.IsAdopted.Should().Be(originalIsAdopted);
    }
}
