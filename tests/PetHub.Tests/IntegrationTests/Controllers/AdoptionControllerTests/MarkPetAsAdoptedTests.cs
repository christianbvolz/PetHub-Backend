using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using PetHub.API.Data;
using PetHub.API.DTOs.Common;
using PetHub.API.DTOs.Pet;
using PetHub.API.Enums;
using PetHub.API.Models;
using PetHub.Tests.IntegrationTests.Helpers;
using PetHub.Tests.IntegrationTests.Infrastructure;

namespace PetHub.Tests.IntegrationTests.Controllers.AdoptionControllerTests;

/// <summary>
/// Integration tests for the MarkPetAsAdopted endpoint (POST /api/adoption/pets/{petId}/mark-adopted)
/// Tests marking pets as adopted outside the platform
/// </summary>
public class MarkPetAsAdoptedTests : IClassFixture<PetHubWebApplicationFactory>, IAsyncLifetime
{
    private readonly HttpClient _client;
    private readonly PetHubWebApplicationFactory _factory;
    private string _ownerToken = string.Empty;
    private string _otherUserToken = string.Empty;
    private int _testPetId;

    public MarkPetAsAdoptedTests(PetHubWebApplicationFactory factory)
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
            "markowner@example.com"
        );
        _client.AddAuthToken(_ownerToken);

        var species = dbContext.Species.First();
        var breed = dbContext.Breeds.First(b => b.SpeciesId == species.Id);

        var createDto = new CreatePetDto
        {
            Name = "Pet To Mark Adopted",
            SpeciesId = species.Id,
            BreedId = breed.Id,
            Gender = PetGender.Male,
            Size = PetSize.Medium,
            AgeInMonths = 24,
            ImageUrls = new List<string> { "https://example.com/mark.jpg" },
        };

        var response = await _client.PostAsJsonAsync("/api/pets", createDto);
        var createdPet = await response.ReadApiResponseDataAsync<PetResponseDto>();
        _testPetId = createdPet!.Id;

        // Register another user for ownership tests
        _otherUserToken = await AuthenticationHelper.RegisterAndGetTokenAsync(
            _client,
            "markother@example.com"
        );
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task MarkPetAsAdopted_AsOwner_ReturnsOk()
    {
        // Arrange
        _client.AddAuthToken(_ownerToken);

        // Act
        var response = await _client.PostAsJsonAsync(
            $"/api/adoption/pets/{_testPetId}/mark-adopted",
            new { }
        );

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task MarkPetAsAdopted_MarksPetAsAdopted()
    {
        // Arrange
        _client.AddAuthToken(_ownerToken);

        // Act
        await _client.PostAsJsonAsync($"/api/adoption/pets/{_testPetId}/mark-adopted", new { });

        // Verify pet is marked as adopted
        var petResponse = await _client.GetAsync($"/api/pets/{_testPetId}");
        var pet = await petResponse.ReadApiResponseDataAsync<PetResponseDto>();

        // Assert
        pet.Should().NotBeNull();
        pet!.IsAdopted.Should().BeTrue();
    }

    [Fact]
    public async Task MarkPetAsAdopted_RejectsAllPendingRequests()
    {
        // Arrange
        // Create real adopter users
        var adopter1Token = await AuthenticationHelper.RegisterAndGetTokenAsync(
            _client,
            "markadopter1@example.com"
        );
        var adopter2Token = await AuthenticationHelper.RegisterAndGetTokenAsync(
            _client,
            "markadopter2@example.com"
        );

        _client.AddAuthToken(adopter1Token);
        var adopter1Response = await _client.GetAsync("/api/users/me");
        var adopter1 =
            await adopter1Response.ReadApiResponseDataAsync<PetHub.API.DTOs.User.UserResponseDto>();

        _client.AddAuthToken(adopter2Token);
        var adopter2Response = await _client.GetAsync("/api/users/me");
        var adopter2 =
            await adopter2Response.ReadApiResponseDataAsync<PetHub.API.DTOs.User.UserResponseDto>();

        int request1Id,
            request2Id;

        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            // Create pending requests
            var request1 = new AdoptionRequest
            {
                PetId = _testPetId,
                AdopterId = adopter1!.Id,
                Message = "Request 1",
                Status = AdoptionStatus.Pending,
            };

            var request2 = new AdoptionRequest
            {
                PetId = _testPetId,
                AdopterId = adopter2!.Id,
                Message = "Request 2",
                Status = AdoptionStatus.Pending,
            };

            dbContext.AdoptionRequests.AddRange(request1, request2);
            await dbContext.SaveChangesAsync();

            request1Id = request1.Id;
            request2Id = request2.Id;
        }

        _client.AddAuthToken(_ownerToken);

        // Act
        await _client.PostAsJsonAsync($"/api/adoption/pets/{_testPetId}/mark-adopted", new { });

        // Verify all requests are rejected in a new scope
        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var request1After = await dbContext.AdoptionRequests.FindAsync(request1Id);
            var request2After = await dbContext.AdoptionRequests.FindAsync(request2Id);

            // Assert
            request1After!.Status.Should().Be(AdoptionStatus.Rejected);
            request2After!.Status.Should().Be(AdoptionStatus.Rejected);
        }
    }

    [Fact]
    public async Task MarkPetAsAdopted_WithNonExistentPet_ReturnsBadRequest()
    {
        // Arrange
        _client.AddAuthToken(_ownerToken);

        // Act
        var response = await _client.PostAsJsonAsync(
            "/api/adoption/pets/99999/mark-adopted",
            new { }
        );

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var apiResponse = await response.ReadApiResponseAsync<object>();
        apiResponse!.Errors.Should().Contain(e => e.Contains("not found"));
    }

    [Fact]
    public async Task MarkPetAsAdopted_AsNonOwner_ReturnsBadRequest()
    {
        // Arrange
        _client.AddAuthToken(_otherUserToken);

        // Act
        var response = await _client.PostAsJsonAsync(
            $"/api/adoption/pets/{_testPetId}/mark-adopted",
            new { }
        );

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        var apiResponse = await response.ReadApiResponseAsync<object>();
        apiResponse!.Errors.Should().Contain(e => e.Contains("permission"));
    }

    [Fact]
    public async Task MarkPetAsAdopted_WithoutAuthentication_ReturnsUnauthorized()
    {
        // Arrange
        var clientWithoutAuth = _factory.CreateClient();

        // Act
        var response = await clientWithoutAuth.PostAsJsonAsync(
            $"/api/adoption/pets/{_testPetId}/mark-adopted",
            new { }
        );

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task MarkPetAsAdopted_ReturnsSuccessMessage()
    {
        // Arrange
        _client.AddAuthToken(_ownerToken);

        // Act
        var response = await _client.PostAsJsonAsync(
            $"/api/adoption/pets/{_testPetId}/mark-adopted",
            new { }
        );

        // Assert
        var apiResponse = await response.ReadApiResponseAsync<object>();
        apiResponse.Should().NotBeNull();
        apiResponse!.Success.Should().BeTrue();
        apiResponse.Message.Should().Contain("marked as adopted successfully");
    }

    [Fact]
    public async Task MarkPetAsAdopted_WhenNoPendingRequests_StillWorks()
    {
        // Arrange
        _client.AddAuthToken(_ownerToken);

        // Act
        var response = await _client.PostAsJsonAsync(
            $"/api/adoption/pets/{_testPetId}/mark-adopted",
            new { }
        );

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var petResponse = await _client.GetAsync($"/api/pets/{_testPetId}");
        var pet = await petResponse.ReadApiResponseDataAsync<PetResponseDto>();
        pet!.IsAdopted.Should().BeTrue();
    }

    [Fact]
    public async Task MarkPetAsAdopted_CanBeCalledOnAlreadyAdoptedPet()
    {
        // Arrange
        _client.AddAuthToken(_ownerToken);

        // Mark pet as adopted first time
        await _client.PostAsJsonAsync($"/api/adoption/pets/{_testPetId}/mark-adopted", new { });

        // Act - Try to mark again
        var response = await _client.PostAsJsonAsync(
            $"/api/adoption/pets/{_testPetId}/mark-adopted",
            new { }
        );

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task MarkPetAsAdopted_DoesNotAffectOtherPets()
    {
        // Arrange
        _client.AddAuthToken(_ownerToken);

        // Create another pet
        var createDto = new CreatePetDto
        {
            Name = "Other Pet",
            SpeciesId = 1,
            BreedId = 1,
            Gender = PetGender.Female,
            Size = PetSize.Small,
            AgeInMonths = 12,
            ImageUrls = new List<string> { "https://example.com/other.jpg" },
        };

        var petResponse = await _client.PostAsJsonAsync("/api/pets", createDto);
        var otherPet = await petResponse.ReadApiResponseDataAsync<PetResponseDto>();

        // Act - Mark first pet as adopted
        await _client.PostAsJsonAsync($"/api/adoption/pets/{_testPetId}/mark-adopted", new { });

        // Assert - Other pet should not be adopted
        var otherPetResponse = await _client.GetAsync($"/api/pets/{otherPet!.Id}");
        var otherPetData = await otherPetResponse.ReadApiResponseDataAsync<PetResponseDto>();
        otherPetData!.IsAdopted.Should().BeFalse();
    }

    [Fact]
    public async Task MarkPetAsAdopted_DoesNotRejectRequestsOfOtherPets()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Create another pet with a pending request
        _client.AddAuthToken(_ownerToken);
        var createDto = new CreatePetDto
        {
            Name = "Other Pet",
            SpeciesId = 1,
            BreedId = 1,
            Gender = PetGender.Female,
            Size = PetSize.Small,
            AgeInMonths = 12,
            ImageUrls = new List<string> { "https://example.com/other.jpg" },
        };

        var petResponse = await _client.PostAsJsonAsync("/api/pets", createDto);
        var otherPet = await petResponse.ReadApiResponseDataAsync<PetResponseDto>();

        var otherPetRequest = new AdoptionRequest
        {
            PetId = otherPet!.Id,
            AdopterId = Guid.NewGuid(),
            Message = "Other pet request",
            Status = AdoptionStatus.Pending,
        };

        dbContext.AdoptionRequests.Add(otherPetRequest);
        await dbContext.SaveChangesAsync();

        // Act - Mark first pet as adopted
        await _client.PostAsJsonAsync($"/api/adoption/pets/{_testPetId}/mark-adopted", new { });

        // Assert - Other pet's request should still be pending
        var otherRequestAfter = await dbContext.AdoptionRequests.FindAsync(otherPetRequest.Id);
        otherRequestAfter!.Status.Should().Be(AdoptionStatus.Pending);
    }

    [Fact]
    public async Task MarkPetAsAdopted_DoesNotCreateApprovedRequest()
    {
        // Arrange
        _client.AddAuthToken(_ownerToken);

        // Act
        await _client.PostAsJsonAsync($"/api/adoption/pets/{_testPetId}/mark-adopted", new { });

        // Assert - No approved request should exist (adoption was external)
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var approvedRequests = dbContext
            .AdoptionRequests.Where(r =>
                r.PetId == _testPetId && r.Status == AdoptionStatus.Approved
            )
            .ToList();

        approvedRequests.Should().BeEmpty();
    }
}
