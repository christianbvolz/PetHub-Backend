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
/// Integration tests for the DeletePet endpoint (DELETE /api/pets/{id})
/// Tests ownership validation and deletion functionality
/// </summary>
public class DeletePetTests : IClassFixture<PetHubWebApplicationFactory>, IAsyncLifetime
{
    private readonly HttpClient _client;
    private readonly PetHubWebApplicationFactory _factory;
    private string _ownerToken = string.Empty;
    private string _otherUserToken = string.Empty;
    private int _testPetId;

    public DeletePetTests(PetHubWebApplicationFactory factory)
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
            "deleteowner@example.com"
        );
        _client.AddAuthToken(_ownerToken);

        var species = dbContext.Species.First();
        var breed = dbContext.Breeds.First(b => b.SpeciesId == species.Id);

        var createDto = new CreatePetDto
        {
            Name = "Pet To Delete",
            SpeciesId = species.Id,
            BreedId = breed.Id,
            Gender = PetGender.Male,
            Size = PetSize.Medium,
            AgeInMonths = 24,
            ImageUrls = new List<string> { "https://example.com/delete.jpg" },
        };

        var response = await _client.PostAsJsonAsync("/api/pets", createDto);
        var createdPet = await response.ReadApiResponseDataAsync<PetResponseDto>();
        _testPetId = createdPet!.Id;

        // Register another user for ownership tests
        _otherUserToken = await AuthenticationHelper.RegisterAndGetTokenAsync(
            _client,
            "deleteother@example.com"
        );
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task DeletePet_WithValidOwner_ReturnsOk()
    {
        // Arrange
        _client.AddAuthToken(_ownerToken);

        // Act
        var response = await _client.DeleteAsync($"/api/pets/{_testPetId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task DeletePet_RemovesPetFromDatabase()
    {
        // Arrange
        _client.AddAuthToken(_ownerToken);

        // Act
        var deleteResponse = await _client.DeleteAsync($"/api/pets/{_testPetId}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify pet is deleted
        var getResponse = await _client.GetAsync($"/api/pets/{_testPetId}");

        // Assert
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeletePet_ReturnsSuccessMessage()
    {
        // Arrange
        _client.AddAuthToken(_ownerToken);

        // Act
        var response = await _client.DeleteAsync($"/api/pets/{_testPetId}");

        // Assert
        var apiResponse = await response.ReadApiResponseAsync<object>();
        apiResponse.Should().NotBeNull();
        apiResponse!.Success.Should().BeTrue();
        apiResponse.Message.Should().Contain("deleted successfully");
    }

    [Fact]
    public async Task DeletePet_WithNonExistentPet_ReturnsNotFound()
    {
        // Arrange
        _client.AddAuthToken(_ownerToken);

        // Act
        var response = await _client.DeleteAsync("/api/pets/99999");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var apiResponse = await response.ReadApiResponseAsync<object>();
        apiResponse!.Errors.Should().Contain(e => e.Contains("not found"));
    }

    [Fact]
    public async Task DeletePet_WithoutOwnership_ReturnsForbidden()
    {
        // Arrange
        _client.AddAuthToken(_otherUserToken); // Different user

        // Act
        var response = await _client.DeleteAsync($"/api/pets/{_testPetId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        var apiResponse = await response.ReadApiResponseAsync<object>();
        apiResponse!.Errors.Should().Contain(e => e.Contains("permission"));
    }

    [Fact]
    public async Task DeletePet_WithoutAuthentication_ReturnsUnauthorized()
    {
        // Arrange
        var clientWithoutAuth = _factory.CreateClient();

        // Act
        var response = await clientWithoutAuth.DeleteAsync($"/api/pets/{_testPetId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task DeletePet_DoesNotAffectOtherUsersPets()
    {
        // Arrange
        _client.AddAuthToken(_ownerToken);

        // Create another pet for the same owner
        var createDto = new CreatePetDto
        {
            Name = "Second Pet",
            SpeciesId = 1,
            BreedId = 1,
            Gender = PetGender.Female,
            Size = PetSize.Small,
            AgeInMonths = 12,
            ImageUrls = new List<string> { "https://example.com/second.jpg" },
        };

        var createResponse = await _client.PostAsJsonAsync("/api/pets", createDto);
        var secondPet = await createResponse.ReadApiResponseDataAsync<PetResponseDto>();

        // Act - Delete first pet
        await _client.DeleteAsync($"/api/pets/{_testPetId}");

        // Assert - Second pet still exists
        var getResponse = await _client.GetAsync($"/api/pets/{secondPet!.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task DeletePet_CannotBeDeletedTwice()
    {
        // Arrange
        _client.AddAuthToken(_ownerToken);

        // Act - Delete once
        var firstDelete = await _client.DeleteAsync($"/api/pets/{_testPetId}");
        firstDelete.StatusCode.Should().Be(HttpStatusCode.OK);

        // Try to delete again
        var secondDelete = await _client.DeleteAsync($"/api/pets/{_testPetId}");

        // Assert
        secondDelete.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeletePet_WithDifferentUser_DoesNotDelete()
    {
        // Arrange
        _client.AddAuthToken(_otherUserToken);

        // Act
        var deleteResponse = await _client.DeleteAsync($"/api/pets/{_testPetId}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        // Verify pet still exists
        _client.AddAuthToken(_ownerToken);
        var getResponse = await _client.GetAsync($"/api/pets/{_testPetId}");

        // Assert
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
