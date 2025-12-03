using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using PetHub.API.Data;
using PetHub.API.DTOs.Adoption;
using PetHub.API.DTOs.Common;
using PetHub.API.DTOs.Pet;
using PetHub.API.DTOs.User;
using PetHub.API.Enums;
using PetHub.API.Models;
using PetHub.Tests.IntegrationTests.Helpers;
using PetHub.Tests.IntegrationTests.Infrastructure;

namespace PetHub.Tests.IntegrationTests.Controllers.AdoptionControllerTests;

/// <summary>
/// Integration tests for the GetPetAdoptionRequests endpoint (GET /api/adoption/pets/{petId}/requests)
/// Tests retrieving pending adoption requests for a pet
/// </summary>
public class GetPetAdoptionRequestsTests
    : IClassFixture<PetHubWebApplicationFactory>,
        IAsyncLifetime
{
    private readonly HttpClient _client;
    private readonly PetHubWebApplicationFactory _factory;
    private string _ownerToken = string.Empty;
    private string _adopter1Token = string.Empty;
    private string _adopter2Token = string.Empty;
    private string _otherUserToken = string.Empty;
    private int _testPetId;
    private Guid _adopter1Id;
    private Guid _adopter2Id;

    public GetPetAdoptionRequestsTests(PetHubWebApplicationFactory factory)
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
            "petowner@example.com"
        );
        _client.AddAuthToken(_ownerToken);

        var species = dbContext.Species.First();
        var breed = dbContext.Breeds.First(b => b.SpeciesId == species.Id);

        var createDto = new CreatePetDto
        {
            Name = "Pet With Requests",
            SpeciesId = species.Id,
            BreedId = breed.Id,
            Gender = PetGender.Male,
            Size = PetSize.Medium,
            AgeInMonths = 24,
            ImageUrls = new List<string> { "https://example.com/pet.jpg" },
        };

        var response = await _client.PostAsJsonAsync("/api/pets", createDto);
        var createdPet = await response.ReadApiResponseDataAsync<PetResponseDto>();
        _testPetId = createdPet!.Id;

        // Register adopters
        _adopter1Token = await AuthenticationHelper.RegisterAndGetTokenAsync(
            _client,
            "adopter1@example.com"
        );

        _adopter2Token = await AuthenticationHelper.RegisterAndGetTokenAsync(
            _client,
            "adopter2@example.com"
        );

        // Get adopter IDs
        _client.AddAuthToken(_adopter1Token);
        var adopter1Response = await _client.GetAsync("/api/users/me");
        var adopter1 = await adopter1Response.ReadApiResponseDataAsync<UserResponseDto>();
        _adopter1Id = adopter1!.Id;

        _client.AddAuthToken(_adopter2Token);
        var adopter2Response = await _client.GetAsync("/api/users/me");
        var adopter2 = await adopter2Response.ReadApiResponseDataAsync<UserResponseDto>();
        _adopter2Id = adopter2!.Id;

        // Create adoption requests
        var request1 = new AdoptionRequest
        {
            PetId = _testPetId,
            AdopterId = _adopter1Id,
            Message = "I have a big house!",
            Status = AdoptionStatus.Pending,
        };

        var request2 = new AdoptionRequest
        {
            PetId = _testPetId,
            AdopterId = _adopter2Id,
            Message = "I love animals!",
            Status = AdoptionStatus.Pending,
        };

        dbContext.AdoptionRequests.AddRange(request1, request2);
        await dbContext.SaveChangesAsync();

        // Register another user for ownership tests
        _otherUserToken = await AuthenticationHelper.RegisterAndGetTokenAsync(
            _client,
            "otheruser@example.com"
        );
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task GetPetAdoptionRequests_AsOwner_ReturnsOk()
    {
        // Arrange
        _client.AddAuthToken(_ownerToken);

        // Act
        var response = await _client.GetAsync($"/api/adoption/pets/{_testPetId}/requests");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetPetAdoptionRequests_ReturnsAllPendingRequests()
    {
        // Arrange
        _client.AddAuthToken(_ownerToken);

        // Act
        var response = await _client.GetAsync($"/api/adoption/pets/{_testPetId}/requests");

        // Assert
        var requests = await response.ReadApiResponseDataAsync<List<AdoptionRequestResponseDto>>();
        requests.Should().NotBeNull();
        requests!.Should().HaveCount(2);
        requests.Should().OnlyContain(r => r.Status == AdoptionStatus.Pending);
    }

    [Fact]
    public async Task GetPetAdoptionRequests_IncludesAdopterInformation()
    {
        // Arrange
        _client.AddAuthToken(_ownerToken);

        // Act
        var response = await _client.GetAsync($"/api/adoption/pets/{_testPetId}/requests");

        // Assert
        var requests = await response.ReadApiResponseDataAsync<List<AdoptionRequestResponseDto>>();
        requests.Should().NotBeNull();

        var request1 = requests!.First(r => r.AdopterId == _adopter1Id);
        request1.AdopterName.Should().NotBeNullOrEmpty();
        request1.AdopterEmail.Should().Be("adopter1@example.com");
        request1.Message.Should().Be("I have a big house!");

        var request2 = requests!.First(r => r.AdopterId == _adopter2Id);
        request2.AdopterName.Should().NotBeNullOrEmpty();
        request2.AdopterEmail.Should().Be("adopter2@example.com");
        request2.Message.Should().Be("I love animals!");
    }

    [Fact]
    public async Task GetPetAdoptionRequests_AsNonOwner_ReturnsEmptyList()
    {
        // Arrange
        _client.AddAuthToken(_otherUserToken);

        // Act
        var response = await _client.GetAsync($"/api/adoption/pets/{_testPetId}/requests");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var requests = await response.ReadApiResponseDataAsync<List<AdoptionRequestResponseDto>>();
        requests.Should().NotBeNull();
        requests!.Should().BeEmpty(); // Returns empty list when not owner
    }

    [Fact]
    public async Task GetPetAdoptionRequests_WithoutAuthentication_ReturnsUnauthorized()
    {
        // Arrange
        var clientWithoutAuth = _factory.CreateClient();

        // Act
        var response = await clientWithoutAuth.GetAsync(
            $"/api/adoption/pets/{_testPetId}/requests"
        );

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetPetAdoptionRequests_ForNonExistentPet_ReturnsEmptyList()
    {
        // Arrange
        _client.AddAuthToken(_ownerToken);

        // Act
        var response = await _client.GetAsync("/api/adoption/pets/99999/requests");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var requests = await response.ReadApiResponseDataAsync<List<AdoptionRequestResponseDto>>();
        requests.Should().NotBeNull();
        requests!.Should().BeEmpty(); // Returns empty list when pet doesn't exist
    }

    [Fact]
    public async Task GetPetAdoptionRequests_WhenNoPendingRequests_ReturnsEmptyList()
    {
        // Arrange
        _client.AddAuthToken(_ownerToken);

        // Create a new pet without requests
        var createDto = new CreatePetDto
        {
            Name = "Pet Without Requests",
            SpeciesId = 1,
            BreedId = 1,
            Gender = PetGender.Female,
            Size = PetSize.Small,
            AgeInMonths = 12,
            ImageUrls = new List<string> { "https://example.com/noreq.jpg" },
        };

        var createResponse = await _client.PostAsJsonAsync("/api/pets", createDto);
        var newPet = await createResponse.ReadApiResponseDataAsync<PetResponseDto>();

        // Act
        var response = await _client.GetAsync($"/api/adoption/pets/{newPet!.Id}/requests");

        // Assert
        var requests = await response.ReadApiResponseDataAsync<List<AdoptionRequestResponseDto>>();
        requests.Should().NotBeNull();
        requests!.Should().BeEmpty();
    }

    [Fact]
    public async Task GetPetAdoptionRequests_DoesNotReturnRejectedRequests()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Add a rejected request
        var rejectedRequest = new AdoptionRequest
        {
            PetId = _testPetId,
            AdopterId = Guid.NewGuid(),
            Message = "This was rejected",
            Status = AdoptionStatus.Rejected,
        };

        dbContext.AdoptionRequests.Add(rejectedRequest);
        await dbContext.SaveChangesAsync();

        _client.AddAuthToken(_ownerToken);

        // Act
        var response = await _client.GetAsync($"/api/adoption/pets/{_testPetId}/requests");

        // Assert
        var requests = await response.ReadApiResponseDataAsync<List<AdoptionRequestResponseDto>>();
        requests.Should().NotBeNull();
        requests!.Should().HaveCount(2); // Only the 2 pending requests
        requests.Should().NotContain(r => r.Message == "This was rejected");
    }

    [Fact]
    public async Task GetPetAdoptionRequests_DoesNotReturnApprovedRequests()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Add an approved request
        var approvedRequest = new AdoptionRequest
        {
            PetId = _testPetId,
            AdopterId = Guid.NewGuid(),
            Message = "This was approved",
            Status = AdoptionStatus.Approved,
        };

        dbContext.AdoptionRequests.Add(approvedRequest);
        await dbContext.SaveChangesAsync();

        _client.AddAuthToken(_ownerToken);

        // Act
        var response = await _client.GetAsync($"/api/adoption/pets/{_testPetId}/requests");

        // Assert
        var requests = await response.ReadApiResponseDataAsync<List<AdoptionRequestResponseDto>>();
        requests.Should().NotBeNull();
        requests!.Should().HaveCount(2); // Only the 2 pending requests
        requests.Should().NotContain(r => r.Message == "This was approved");
    }
}
