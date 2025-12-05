using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using PetHub.API.Data;
using PetHub.API.DTOs.Adoption;
using PetHub.API.DTOs.Common;
using PetHub.API.DTOs.Pet;
using PetHub.API.Enums;
using PetHub.API.Models;
using PetHub.Tests;
using PetHub.Tests.Extensions;
using PetHub.Tests.IntegrationTests.Helpers;
using PetHub.Tests.IntegrationTests.Infrastructure;

namespace PetHub.Tests.IntegrationTests.Controllers.AdoptionControllerTests;

/// <summary>
/// Integration tests for the ApproveAdoptionRequest endpoint (PATCH /api/adoption/requests/{requestId}/approve)
/// Tests approval workflow, pet adoption marking, and automatic rejection of other requests
/// </summary>
public class ApproveAdoptionRequestTests
    : IClassFixture<PetHubWebApplicationFactory>,
        IAsyncLifetime
{
    private readonly HttpClient _client;
    private readonly PetHubWebApplicationFactory _factory;
    private string _ownerToken = string.Empty;
    private string _otherUserToken = string.Empty;
    private int _testPetId;
    private int _request1Id;
    private int _request2Id;
    private int _request3Id;

    public ApproveAdoptionRequestTests(PetHubWebApplicationFactory factory)
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
        _ownerToken = await AuthenticationHelper.RegisterAndGetTokenAsync(_client);
        _client.AddAuthToken(_ownerToken);

        var createDto = TestConstants.DtoBuilders.CreateValidPetDto();

        var response = await _client.PostAsJsonAsync(TestConstants.ApiPaths.Pets, createDto);
        var createdPet = await response.ReadApiResponseDataAsync<PetResponseDto>();
        _testPetId = createdPet!.Id;

        // Create real adopter users
        var adopter1Token = await AuthenticationHelper.RegisterAndGetTokenAsync(_client);
        var adopter2Token = await AuthenticationHelper.RegisterAndGetTokenAsync(_client);
        var adopter3Token = await AuthenticationHelper.RegisterAndGetTokenAsync(_client);

        // Get adopter IDs
        _client.AddAuthToken(adopter1Token);
        var adopter1Response = await _client.GetAsync(TestConstants.ApiPaths.UsersMe);
        var adopter1 =
            await adopter1Response.ReadApiResponseDataAsync<PetHub.API.DTOs.User.UserResponseDto>();

        _client.AddAuthToken(adopter2Token);
        var adopter2Response = await _client.GetAsync(TestConstants.ApiPaths.UsersMe);
        var adopter2 =
            await adopter2Response.ReadApiResponseDataAsync<PetHub.API.DTOs.User.UserResponseDto>();

        _client.AddAuthToken(adopter3Token);
        var adopter3Response = await _client.GetAsync(TestConstants.ApiPaths.UsersMe);
        var adopter3 =
            await adopter3Response.ReadApiResponseDataAsync<PetHub.API.DTOs.User.UserResponseDto>();

        // Create multiple adoption requests in a new scope
        using (var newScope = _factory.Services.CreateScope())
        {
            var newDbContext = newScope.ServiceProvider.GetRequiredService<AppDbContext>();

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

            var request3 = new AdoptionRequest
            {
                PetId = _testPetId,
                AdopterId = adopter3!.Id,
                Message = "Request 3",
                Status = AdoptionStatus.Pending,
            };

            newDbContext.AdoptionRequests.AddRange(request1, request2, request3);
            await newDbContext.SaveChangesAsync();

            _request1Id = request1.Id;
            _request2Id = request2.Id;
            _request3Id = request3.Id;
        }

        // Register another user for ownership tests
        _otherUserToken = await AuthenticationHelper.RegisterAndGetTokenAsync(_client);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task ApproveAdoptionRequest_AsOwner_ReturnsOk()
    {
        // Arrange
        _client.AddAuthToken(_ownerToken);

        // Act
        var response = await _client.PatchAsJsonAsync(
            TestConstants.ApiPaths.AdoptionRequestApprove(_request1Id),
            new { }
        );

        // Assert
        response.ShouldBeOk();
    }

    [Fact]
    public async Task ApproveAdoptionRequest_ReturnsApprovedRequest()
    {
        // Arrange
        _client.AddAuthToken(_ownerToken);

        // Act
        var response = await _client.PatchAsJsonAsync(
            TestConstants.ApiPaths.AdoptionRequestApprove(_request1Id),
            new { }
        );

        // Assert
        var approvedRequest = await response.ReadApiResponseDataAsync<AdoptionRequestResponseDto>();
        approvedRequest.Should().NotBeNull();
        approvedRequest!.Id.Should().Be(_request1Id);
        approvedRequest.Status.Should().Be(AdoptionStatus.Approved);
    }

    [Fact]
    public async Task ApproveAdoptionRequest_MarksPetAsAdopted()
    {
        // Arrange
        _client.AddAuthToken(_ownerToken);

        // Act
        await _client.PatchAsJsonAsync(
            TestConstants.ApiPaths.AdoptionRequestApprove(_request1Id),
            new { }
        );

        // Verify pet is marked as adopted
        var petResponse = await _client.GetAsync(TestConstants.ApiPaths.PetById(_testPetId));
        var pet = await petResponse.ReadApiResponseDataAsync<PetResponseDto>();

        // Assert
        pet.Should().NotBeNull();
        pet!.IsAdopted.Should().BeTrue();
    }

    [Fact]
    public async Task ApproveAdoptionRequest_RejectsOtherPendingRequests()
    {
        // Arrange
        _client.AddAuthToken(_ownerToken);

        // Act
        await _client.PatchAsJsonAsync(
            TestConstants.ApiPaths.AdoptionRequestApprove(_request1Id),
            new { }
        );

        // Verify other requests are rejected
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var request2 = await dbContext.AdoptionRequests.FindAsync(_request2Id);
        var request3 = await dbContext.AdoptionRequests.FindAsync(_request3Id);

        // Assert
        request2!.Status.Should().Be(AdoptionStatus.Rejected);
        request3!.Status.Should().Be(AdoptionStatus.Rejected);
    }

    [Fact]
    public async Task ApproveAdoptionRequest_WithNonExistentRequest_ReturnsBadRequest()
    {
        // Arrange
        _client.AddAuthToken(_ownerToken);

        // Act
        var response = await _client.PatchAsJsonAsync(
            TestConstants.ApiPaths.AdoptionRequestApprove(TestConstants.NonExistentIds.Generic),
            new { }
        );

        // Assert
        response.ShouldBeNotFound();
        var apiResponse = await response.ReadApiResponseAsync<object>();
        apiResponse!.Errors.Should().Contain(e => e.Contains("not found"));
    }

    [Fact]
    public async Task ApproveAdoptionRequest_AsNonOwner_ReturnsBadRequest()
    {
        // Arrange
        _client.AddAuthToken(_otherUserToken);

        // Act
        var response = await _client.PatchAsJsonAsync(
            TestConstants.ApiPaths.AdoptionRequestApprove(_request1Id),
            new { }
        );

        // Assert
        response.ShouldBeForbidden();
        var apiResponse = await response.ReadApiResponseAsync<object>();
        apiResponse!.Errors.Should().Contain(e => e.Contains("permission"));
    }

    [Fact]
    public async Task ApproveAdoptionRequest_WithoutAuthentication_ReturnsUnauthorized()
    {
        // Arrange
        var clientWithoutAuth = _factory.CreateClient();

        // Act
        var response = await clientWithoutAuth.PatchAsJsonAsync(
            TestConstants.ApiPaths.AdoptionRequestApprove(_request1Id),
            new { }
        );

        // Assert
        response.ShouldBeUnauthorized();
    }

    [Fact]
    public async Task ApproveAdoptionRequest_ReturnsSuccessMessage()
    {
        // Arrange
        _client.AddAuthToken(_ownerToken);

        // Act
        var response = await _client.PatchAsJsonAsync(
            TestConstants.ApiPaths.AdoptionRequestApprove(_request1Id),
            new { }
        );

        // Assert
        var apiResponse = await response.ReadApiResponseAsync<AdoptionRequestResponseDto>();
        apiResponse.Should().NotBeNull();
        apiResponse!.Success.Should().BeTrue();
        apiResponse.Message.Should().Contain("approved successfully");
        apiResponse.Message.Should().Contain("marked as adopted");
    }

    [Fact]
    public async Task ApproveAdoptionRequest_OnlyApprovesOneRequest()
    {
        // Arrange
        _client.AddAuthToken(_ownerToken);

        // Act
        await _client.PatchAsJsonAsync(
            TestConstants.ApiPaths.AdoptionRequestApprove(_request2Id),
            new { }
        );

        // Verify only request2 is approved
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var approvedRequests = dbContext
            .AdoptionRequests.Where(r =>
                r.PetId == _testPetId && r.Status == AdoptionStatus.Approved
            )
            .ToList();

        // Assert
        approvedRequests.Should().HaveCount(1);
        approvedRequests.First().Id.Should().Be(_request2Id);
    }

    [Fact]
    public async Task ApproveAdoptionRequest_CannotApproveAlreadyRejectedRequest()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var request = await dbContext.AdoptionRequests.FindAsync(_request1Id);
        request!.Status = AdoptionStatus.Rejected;
        await dbContext.SaveChangesAsync();

        _client.AddAuthToken(_ownerToken);

        // Act
        var response = await _client.PatchAsJsonAsync(
            TestConstants.ApiPaths.AdoptionRequestApprove(_request1Id),
            new { }
        );

        // Assert - Should fail because request is not pending
        response.ShouldBeBadRequest();
    }

    [Fact]
    public async Task ApproveAdoptionRequest_DoesNotAffectRequestsOfOtherPets()
    {
        // Arrange
        _client.AddAuthToken(_ownerToken);

        // Create another pet with a request
        var createDto = TestConstants.DtoBuilders.CreateValidPetDto(name: "Other Pet");

        var petResponse = await _client.PostAsJsonAsync(TestConstants.ApiPaths.Pets, createDto);
        var otherPet = await petResponse.ReadApiResponseDataAsync<PetResponseDto>();

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var otherPetRequest = new AdoptionRequest
        {
            PetId = otherPet!.Id,
            AdopterId = Guid.NewGuid(),
            Message = "Other pet request",
            Status = AdoptionStatus.Pending,
        };

        dbContext.AdoptionRequests.Add(otherPetRequest);
        await dbContext.SaveChangesAsync();

        // Act - Approve request for first pet
        await _client.PatchAsJsonAsync(
            TestConstants.ApiPaths.AdoptionRequestApprove(_request1Id),
            new { }
        );

        // Assert - Other pet's request should still be pending
        var otherRequestAfter = await dbContext.AdoptionRequests.FindAsync(otherPetRequest.Id);
        otherRequestAfter!.Status.Should().Be(AdoptionStatus.Pending);
    }
}
