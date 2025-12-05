using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using PetHub.API.DTOs.Common;
using PetHub.API.DTOs.Pet;
using PetHub.API.Enums;
using PetHub.Tests;
using PetHub.Tests.Extensions;
using PetHub.Tests.IntegrationTests.Helpers;
using PetHub.Tests.IntegrationTests.Infrastructure;

namespace PetHub.Tests.IntegrationTests.Controllers.PetsControllerTests;

/// <summary>
/// Integration tests for the DeletePet endpoint (DELETE /api/pets/{id})
/// Tests ownership validation and deletion functionality
/// </summary>
public class DeletePetTests : IntegrationTestBase
{
    private string _ownerToken = string.Empty;
    private string _otherUserToken = string.Empty;
    private int _testPetId;

    public DeletePetTests(PetHubWebApplicationFactory factory)
        : base(factory) { }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();

        // Register owner and create a test pet
        _ownerToken = AuthToken;

        var createDto = TestConstants.DtoBuilders.CreateValidPetDto();

        var response = await Client.PostAsJsonAsync(TestConstants.ApiPaths.Pets, createDto);
        var createdPet = await response.ReadApiResponseDataAsync<PetResponseDto>();

        _testPetId = createdPet!.Id;

        // Register another user for ownership tests
        // Register another user for ownership tests — helper will generate a unique email
        _otherUserToken = await AuthenticationHelper.RegisterAndGetTokenAsync(Client);
    }

    public override Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task DeletePet_WithValidOwner_ReturnsOk()
    {
        // Arrange
        Client.AddAuthToken(_ownerToken);

        // Act
        var response = await Client.DeleteAsync(TestConstants.ApiPaths.PetById(_testPetId));

        // Assert
        response.ShouldBeOk();
    }

    [Fact]
    public async Task DeletePet_RemovesPetFromDatabase()
    {
        // Arrange
        Client.AddAuthToken(_ownerToken);

        // Act
        var deleteResponse = await Client.DeleteAsync(TestConstants.ApiPaths.PetById(_testPetId));
        deleteResponse.ShouldBeOk();

        // Verify pet is deleted
        var getResponse = await Client.GetAsync(TestConstants.ApiPaths.PetById(_testPetId));

        // Assert
        getResponse.ShouldBeNotFound();
    }

    [Fact]
    public async Task DeletePet_ReturnsSuccessMessage()
    {
        // Arrange
        Client.AddAuthToken(_ownerToken);

        // Act
        var response = await Client.DeleteAsync(TestConstants.ApiPaths.PetById(_testPetId));

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
        Client.AddAuthToken(_ownerToken);

        // Act
        var response = await Client.DeleteAsync(
            TestConstants.ApiPaths.PetById(TestConstants.NonExistentIds.Generic)
        );

        // Assert
        response.ShouldBeNotFound();
        var apiResponse = await response.ReadApiResponseAsync<object>();
        apiResponse!.Errors.Should().Contain(e => e.Contains("not found"));
    }

    [Fact]
    public async Task DeletePet_WithoutOwnership_ReturnsForbidden()
    {
        // Arrange
        Client.AddAuthToken(_otherUserToken); // Different user

        // Act
        var response = await Client.DeleteAsync(TestConstants.ApiPaths.PetById(_testPetId));

        // Assert
        response.ShouldBeForbidden();
        var apiResponse = await response.ReadApiResponseAsync<object>();
        apiResponse!.Errors.Should().Contain(e => e.Contains("permission"));
    }

    [Fact]
    public async Task DeletePet_WithoutAuthentication_ReturnsUnauthorized()
    {
        // Arrange
        var clientWithoutAuth = Factory.CreateClient();

        // Act
        var response = await clientWithoutAuth.DeleteAsync(
            TestConstants.ApiPaths.PetById(_testPetId)
        );

        // Assert
        response.ShouldBeUnauthorized();
    }

    [Fact]
    public async Task DeletePet_DoesNotAffectOtherUsersPets()
    {
        // Arrange
        Client.AddAuthToken(_ownerToken);

        // Create another pet for the same owner
        var createDto = TestConstants.DtoBuilders.CreateValidPetDto(name: TestConstants.Pets.Luna);

        var createResponse = await Client.PostAsJsonAsync(TestConstants.ApiPaths.Pets, createDto);
        var secondPet = await createResponse.ReadApiResponseDataAsync<PetResponseDto>();

        // Act - Delete first pet
        await Client.DeleteAsync(TestConstants.ApiPaths.PetById(_testPetId));

        // Assert - Second pet still exists
        var getResponse = await Client.GetAsync(TestConstants.ApiPaths.PetById(secondPet!.Id));
        getResponse.ShouldBeOk();
    }

    [Fact]
    public async Task DeletePet_CannotBeDeletedTwice()
    {
        // Arrange
        Client.AddAuthToken(_ownerToken);

        // Act - Delete once
        var firstDelete = await Client.DeleteAsync(TestConstants.ApiPaths.PetById(_testPetId));
        firstDelete.ShouldBeOk();

        // Try to delete again
        var secondDelete = await Client.DeleteAsync(TestConstants.ApiPaths.PetById(_testPetId));

        // Assert
        secondDelete.ShouldBeNotFound();
    }

    [Fact]
    public async Task DeletePet_WithDifferentUser_DoesNotDelete()
    {
        // Arrange
        Client.AddAuthToken(_otherUserToken);

        // Act
        var deleteResponse = await Client.DeleteAsync(TestConstants.ApiPaths.PetById(_testPetId));
        deleteResponse.ShouldBeForbidden();

        // Verify pet still exists
        Client.AddAuthToken(_ownerToken);
        var getResponse = await Client.GetAsync(TestConstants.ApiPaths.PetById(_testPetId));

        // Assert
        getResponse.ShouldBeOk();
    }
}
