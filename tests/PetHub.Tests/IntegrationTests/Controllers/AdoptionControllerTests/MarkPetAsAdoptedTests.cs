using FluentAssertions;
using PetHub.API.Data;
using PetHub.API.DTOs.Pet;
using PetHub.API.DTOs.User;
using PetHub.API.Enums;
using PetHub.API.Models;
using PetHub.Tests.Extensions;
using PetHub.Tests.IntegrationTests.Helpers;
using PetHub.Tests.IntegrationTests.Infrastructure;

namespace PetHub.Tests.IntegrationTests.Controllers.AdoptionControllerTests;

/// <summary>
/// Integration tests for the MarkPetAsAdopted endpoint (POST /api/adoption/pets/{petId}/mark-adopted)
/// Tests marking pets as adopted outside the platform
/// </summary>
public class MarkPetAsAdoptedTests : IntegrationTestBase
{
    private string _ownerToken = string.Empty;
    private string _otherUserToken = string.Empty;
    private int _testPetId;

    public MarkPetAsAdoptedTests(PetHubWebApplicationFactory factory)
        : base(factory) { }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();

        // Register owner and create a test pet
        _ownerToken = AuthToken;

        var createDto = TestConstants.DtoBuilders.CreateValidPetDto(
            speciesId: DogSpeciesId,
            breedId: FirstBreedId,
            name: TestConstants.Pets.Bella
        );

        var response = await Client.PostAsJsonAsync(TestConstants.ApiPaths.Pets, createDto);
        var createdPet = await response.ReadApiResponseDataAsync<PetResponseDto>();
        _testPetId = createdPet!.Id;

        // Register another user for ownership tests
        _otherUserToken = await AuthenticationHelper.RegisterAndGetTokenAsync(
            Client,
            TestConstants.Users.AnotherEmail
        );
    }

    public override Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task MarkPetAsAdopted_AsOwner_ReturnsOk()
    {
        // Arrange
        Client.AddAuthToken(_ownerToken);

        // Act
        var response = await Client.PostAsJsonAsync(
            TestConstants.ApiPaths.MarkPetAsAdopted(_testPetId),
            new { }
        );

        // Assert
        response.ShouldBeOk();
    }

    [Fact]
    public async Task MarkPetAsAdopted_MarksPetAsAdopted()
    {
        // Arrange
        Client.AddAuthToken(_ownerToken);

        // Act
        await Client.PostAsJsonAsync(TestConstants.ApiPaths.MarkPetAsAdopted(_testPetId), new { });

        // Verify pet is marked as adopted
        var petResponse = await Client.GetAsync(TestConstants.ApiPaths.PetById(_testPetId));
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
            Client,
            TestConstants.Users.GenerateUniqueEmail()
        );
        var adopter2Token = await AuthenticationHelper.RegisterAndGetTokenAsync(
            Client,
            TestConstants.Users.GenerateUniqueEmail()
        );

        Client.AddAuthToken(adopter1Token);
        var adopter1Response = await Client.GetAsync(TestConstants.ApiPaths.UsersMe);
        var adopter1 = await adopter1Response.ReadApiResponseDataAsync<UserResponseDto>();

        Client.AddAuthToken(adopter2Token);
        var adopter2Response = await Client.GetAsync(TestConstants.ApiPaths.UsersMe);
        var adopter2 = await adopter2Response.ReadApiResponseDataAsync<UserResponseDto>();

        int request1Id,
            request2Id;

        using (var scope = Factory.Services.CreateScope())
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

        Client.AddAuthToken(_ownerToken);

        // Act
        await Client.PostAsJsonAsync(TestConstants.ApiPaths.MarkPetAsAdopted(_testPetId), new { });

        // Verify all requests are rejected in a new scope
        using (var scope = Factory.Services.CreateScope())
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
        Client.AddAuthToken(_ownerToken);

        // Act
        var response = await Client.PostAsJsonAsync(
            TestConstants.ApiPaths.MarkPetAsAdopted(9999),
            new { }
        );

        // Assert
        response.ShouldBeNotFound();
        var apiResponse = await response.ReadApiResponseAsync<object>();
        apiResponse!.Errors.Should().Contain(e => e.Contains("not found"));
    }

    [Fact]
    public async Task MarkPetAsAdopted_AsNonOwner_ReturnsBadRequest()
    {
        // Arrange
        Client.AddAuthToken(_otherUserToken);

        // Act
        var response = await Client.PostAsJsonAsync(
            TestConstants.ApiPaths.MarkPetAsAdopted(_testPetId),
            new { }
        );

        // Assert
        response.ShouldBeForbidden();
        var apiResponse = await response.ReadApiResponseAsync<object>();
        apiResponse!.Errors.Should().Contain(e => e.Contains("permission"));
    }

    [Fact]
    public async Task MarkPetAsAdopted_WithoutAuthentication_ReturnsUnauthorized()
    {
        // Arrange
        var clientWithoutAuth = Factory.CreateClient();

        // Act
        var response = await clientWithoutAuth.PostAsJsonAsync(
            TestConstants.ApiPaths.MarkPetAsAdopted(_testPetId),
            new { }
        );

        // Assert
        response.ShouldBeUnauthorized();
    }

    [Fact]
    public async Task MarkPetAsAdopted_ReturnsSuccessMessage()
    {
        // Arrange
        Client.AddAuthToken(_ownerToken);

        // Act
        var response = await Client.PostAsJsonAsync(
            TestConstants.ApiPaths.MarkPetAsAdopted(_testPetId),
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
        Client.AddAuthToken(_ownerToken);

        // Act
        var response = await Client.PostAsJsonAsync(
            TestConstants.ApiPaths.MarkPetAsAdopted(_testPetId),
            new { }
        );

        // Assert
        response.ShouldBeOk();

        var petResponse = await Client.GetAsync(TestConstants.ApiPaths.PetById(_testPetId));
        var pet = await petResponse.ReadApiResponseDataAsync<PetResponseDto>();
        pet!.IsAdopted.Should().BeTrue();
    }

    [Fact]
    public async Task MarkPetAsAdopted_CanBeCalledOnAlreadyAdoptedPet()
    {
        // Arrange
        Client.AddAuthToken(_ownerToken);

        // Mark pet as adopted first time
        await Client.PostAsJsonAsync(TestConstants.ApiPaths.MarkPetAsAdopted(_testPetId), new { });

        // Act - Try to mark again
        var response = await Client.PostAsJsonAsync(
            TestConstants.ApiPaths.MarkPetAsAdopted(_testPetId),
            new { }
        );

        // Assert
        response.ShouldBeOk();
    }

    [Fact]
    public async Task MarkPetAsAdopted_DoesNotAffectOtherPets()
    {
        // Arrange
        Client.AddAuthToken(_ownerToken);

        // Create another pet
        var createDto = TestConstants.DtoBuilders.CreateValidPetDto();

        var petResponse = await Client.PostAsJsonAsync(TestConstants.ApiPaths.Pets, createDto);
        var otherPet = await petResponse.ReadApiResponseDataAsync<PetResponseDto>();

        // Act - Mark first pet as adopted
        await Client.PostAsJsonAsync(TestConstants.ApiPaths.MarkPetAsAdopted(_testPetId), new { });

        // Assert - Other pet should not be adopted
        var otherPetResponse = await Client.GetAsync(TestConstants.ApiPaths.PetById(otherPet!.Id));
        var otherPetData = await otherPetResponse.ReadApiResponseDataAsync<PetResponseDto>();
        otherPetData!.IsAdopted.Should().BeFalse();
    }

    [Fact]
    public async Task MarkPetAsAdopted_DoesNotRejectRequestsOfOtherPets()
    {
        // Arrange
        using var scope = Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Create another pet with a pending request
        Client.AddAuthToken(_ownerToken);
        var createDto = TestConstants.DtoBuilders.CreateValidPetDto();

        var petResponse = await Client.PostAsJsonAsync(TestConstants.ApiPaths.Pets, createDto);
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
        await Client.PostAsJsonAsync(TestConstants.ApiPaths.MarkPetAsAdopted(_testPetId), new { });

        // Assert - Other pet's request should still be pending
        var otherRequestAfter = await dbContext.AdoptionRequests.FindAsync(otherPetRequest.Id);
        otherRequestAfter!.Status.Should().Be(AdoptionStatus.Pending);
    }

    [Fact]
    public async Task MarkPetAsAdopted_DoesNotCreateApprovedRequest()
    {
        // Arrange
        Client.AddAuthToken(_ownerToken);

        // Act
        await Client.PostAsJsonAsync(TestConstants.ApiPaths.MarkPetAsAdopted(_testPetId), new { });

        // Assert - No approved request should exist (adoption was external)
        using var scope = Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var approvedRequests = dbContext
            .AdoptionRequests.Where(r =>
                r.PetId == _testPetId && r.Status == AdoptionStatus.Approved
            )
            .ToList();

        approvedRequests.Should().BeEmpty();
    }
}
