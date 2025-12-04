using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using PetHub.API.Data;
using PetHub.API.DTOs.Common;
using PetHub.API.DTOs.Pet;
using PetHub.API.Enums;
using PetHub.Tests;
using PetHub.Tests.Extensions;
using PetHub.Tests.IntegrationTests.Helpers;
using PetHub.Tests.IntegrationTests.Infrastructure;

namespace PetHub.Tests.IntegrationTests.Controllers.PetsControllerTests;

/// <summary>
/// Integration tests for the UpdatePet endpoint (PATCH /api/pets/{id})
/// Tests ownership validation, partial updates, and data validation
/// </summary>
public class UpdatePetTests : IntegrationTestBase
{
    private string _ownerToken = string.Empty;
    private string _otherUserToken = string.Empty;
    private int _testPetId;

    public UpdatePetTests(PetHubWebApplicationFactory factory)
        : base(factory) { }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();

        // Register owner and create a test pet
        _ownerToken = AuthToken;

        var createDto = TestConstants.DtoBuilders.CreateValidPetDto(
            speciesId: DogSpeciesId,
            breedId: FirstBreedId,
            tagIds: new List<int> { TagIds[0] },
            name: "Original Pet",
            description: "Original description"
        );
        createDto.Gender = PetGender.Male;
        createDto.Size = PetSize.Medium;
        createDto.AgeInMonths = 24;
        createDto.IsCastrated = false;
        createDto.IsVaccinated = false;

        var response = await Client.PostAsJsonAsync(
            TestConstants.IntegrationTests.ApiPaths.Pets,
            createDto
        );
        var createdPet = await response.ReadApiResponseDataAsync<PetResponseDto>();
        _testPetId = createdPet!.Id;

        // Register another user for ownership tests
        _otherUserToken = await AuthenticationHelper.RegisterAndGetTokenAsync(
            Client,
            "otheruser@example.com"
        );
    }

    public override Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task UpdatePet_WithValidData_ReturnsOk()
    {
        // Arrange
        Client.AddAuthToken(_ownerToken);
        var updateDto = TestConstants.DtoBuilders.CreateValidUpdatePetDto(
            name: "Updated Pet Name",
            description: TestConstants.IntegrationTests.Descriptions.Updated
        );

        // Act
        var response = await Client.PatchAsJsonAsync(
            TestConstants.IntegrationTests.ApiPaths.PetById(_testPetId),
            updateDto
        );

        // Assert
        response.ShouldBeOk();
    }

    [Fact]
    public async Task UpdatePet_UpdatesOnlyProvidedFields()
    {
        // Arrange
        Client.AddAuthToken(_ownerToken);
        var updateDto = new UpdatePetDto { Name = "New Name Only" };

        // Act
        var response = await Client.PatchAsJsonAsync(
            TestConstants.IntegrationTests.ApiPaths.PetById(_testPetId),
            updateDto
        );

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
        Client.AddAuthToken(_ownerToken);
        var updateDto = TestConstants.DtoBuilders.CreateValidUpdatePetDto(
            name: "Completely Updated",
            description: "New description"
        );

        // Act
        var response = await Client.PatchAsJsonAsync(
            TestConstants.IntegrationTests.ApiPaths.PetById(_testPetId),
            updateDto
        );

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
        Client.AddAuthToken(_ownerToken);
        using var scope = Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var pet = await dbContext.Pets.FindAsync(_testPetId);
        var anotherBreed = dbContext
            .Breeds.Where(b => b.SpeciesId == pet!.SpeciesId && b.Id != pet.BreedId)
            .First();

        var updateDto = new UpdatePetDto { BreedId = anotherBreed.Id };

        // Act
        var response = await Client.PatchAsJsonAsync(
            TestConstants.IntegrationTests.ApiPaths.PetById(_testPetId),
            updateDto
        );

        // Assert
        var updatedPet = await response.ReadApiResponseDataAsync<PetResponseDto>();
        updatedPet.Should().NotBeNull();
        updatedPet!.BreedName.Should().Be(anotherBreed.Name);
    }

    [Fact]
    public async Task UpdatePet_WithInvalidBreed_ReturnsBadRequest()
    {
        // Arrange
        Client.AddAuthToken(_ownerToken);
        var updateDto = new UpdatePetDto
        {
            BreedId = 99999, // Non-existent breed
        };

        // Act
        var response = await Client.PatchAsJsonAsync(
            TestConstants.IntegrationTests.ApiPaths.PetById(_testPetId),
            updateDto
        );

        // Assert
        response.ShouldBeBadRequest();
        var apiResponse = await response.ReadApiResponseAsync<object>();
        apiResponse!.Errors.Should().Contain(e => e.Contains("Breed") && e.Contains("not found"));
    }

    [Fact]
    public async Task UpdatePet_WithBreedFromDifferentSpecies_ReturnsBadRequest()
    {
        // Arrange
        Client.AddAuthToken(_ownerToken);
        using var scope = Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var pet = await dbContext.Pets.FindAsync(_testPetId);
        var differentSpeciesBreed = dbContext.Breeds.First(b => b.SpeciesId != pet!.SpeciesId);

        var updateDto = new UpdatePetDto { BreedId = differentSpeciesBreed.Id };

        // Act
        var response = await Client.PatchAsJsonAsync(
            TestConstants.IntegrationTests.ApiPaths.PetById(_testPetId),
            updateDto
        );

        // Assert
        response.ShouldBeBadRequest();
        var apiResponse = await response.ReadApiResponseAsync<object>();
        apiResponse!.Errors.Should().Contain(e => e.Contains("doesn't belong"));
    }

    [Fact]
    public async Task UpdatePet_CanUpdateTags()
    {
        // Arrange
        Client.AddAuthToken(_ownerToken);
        using var scope = Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var newTags = dbContext.Tags.Take(3).Select(t => t.Id).ToList();
        var updateDto = new UpdatePetDto { TagIds = newTags };

        // Act
        var response = await Client.PatchAsJsonAsync(
            TestConstants.IntegrationTests.ApiPaths.PetById(_testPetId),
            updateDto
        );

        // Assert
        var updatedPet = await response.ReadApiResponseDataAsync<PetResponseDto>();
        updatedPet.Should().NotBeNull();
        updatedPet!.Tags.Should().HaveCount(3);
    }

    [Fact]
    public async Task UpdatePet_WithInvalidTags_ReturnsBadRequest()
    {
        // Arrange
        Client.AddAuthToken(_ownerToken);
        var updateDto = new UpdatePetDto
        {
            TagIds = new List<int> { TagIds[0], 99999 },
        };

        // Act
        var response = await Client.PatchAsJsonAsync(
            TestConstants.IntegrationTests.ApiPaths.PetById(_testPetId),
            updateDto
        );

        // Assert
        response.ShouldBeBadRequest();
        var apiResponse = await response.ReadApiResponseAsync<object>();
        apiResponse!.Errors.Should().Contain(e => e.Contains("tag"));
    }

    [Fact]
    public async Task UpdatePet_CanUpdateImages()
    {
        // Arrange
        Client.AddAuthToken(_ownerToken);
        var updateDto = new UpdatePetDto
        {
            ImageUrls = new List<string>
            {
                TestConstants.IntegrationTests.ImageUrls.Image1,
                TestConstants.IntegrationTests.ImageUrls.Image2,
            },
        };

        // Act
        var response = await Client.PatchAsJsonAsync(
            TestConstants.IntegrationTests.ApiPaths.PetById(_testPetId),
            updateDto
        );

        // Assert
        var updatedPet = await response.ReadApiResponseDataAsync<PetResponseDto>();
        updatedPet.Should().NotBeNull();
        updatedPet!.ImageUrls.Should().HaveCount(2);
        updatedPet.ImageUrls.Should().Contain(TestConstants.IntegrationTests.ImageUrls.Image1);
    }

    [Fact]
    public async Task UpdatePet_WithNonExistentPet_ReturnsNotFound()
    {
        // Arrange
        Client.AddAuthToken(_ownerToken);
        var updateDto = new UpdatePetDto { Name = "Updated" };

        // Act
        var response = await Client.PatchAsJsonAsync(
            TestConstants.IntegrationTests.ApiPaths.PetById(99999),
            updateDto
        );

        // Assert
        response.ShouldBeNotFound();
    }

    [Fact]
    public async Task UpdatePet_WithoutOwnership_ReturnsForbidden()
    {
        // Arrange
        Client.AddAuthToken(_otherUserToken); // Different user
        var updateDto = new UpdatePetDto { Name = "Trying to update" };

        // Act
        var response = await Client.PatchAsJsonAsync(
            TestConstants.IntegrationTests.ApiPaths.PetById(_testPetId),
            updateDto
        );

        // Assert
        response.ShouldBeForbidden();
        var apiResponse = await response.ReadApiResponseAsync<object>();
        apiResponse!.Errors.Should().Contain(e => e.Contains("permission"));
    }

    [Fact]
    public async Task UpdatePet_WithoutAuthentication_ReturnsUnauthorized()
    {
        // Arrange
        var clientWithoutAuth = Factory.CreateClient();
        var updateDto = new UpdatePetDto { Name = "Should fail" };

        // Act
        var response = await clientWithoutAuth.PatchAsJsonAsync(
            $"/api/pets/{_testPetId}",
            updateDto
        );

        // Assert
        response.ShouldBeUnauthorized();
    }

    [Fact]
    public async Task UpdatePet_WithNullName_DoesNotUpdateName()
    {
        // Arrange
        Client.AddAuthToken(_ownerToken);
        var updateDto = new UpdatePetDto
        {
            Name = null, // Not updating name
        };

        // Act
        var response = await Client.PatchAsJsonAsync(
            TestConstants.IntegrationTests.ApiPaths.PetById(_testPetId),
            updateDto
        );

        // Assert
        var updatedPet = await response.ReadApiResponseDataAsync<PetResponseDto>();
        updatedPet.Should().NotBeNull();
        updatedPet!.Name.Should().Be("Original Pet"); // Name unchanged
    }

    [Fact]
    public async Task UpdatePet_WithNullDescription_DoesNotUpdateDescription()
    {
        // Arrange
        Client.AddAuthToken(_ownerToken);
        var updateDto = new UpdatePetDto { Description = null };

        // Act
        var response = await Client.PatchAsJsonAsync(
            TestConstants.IntegrationTests.ApiPaths.PetById(_testPetId),
            updateDto
        );

        // Assert
        var updatedPet = await response.ReadApiResponseDataAsync<PetResponseDto>();
        updatedPet.Should().NotBeNull();
        updatedPet!.Description.Should().Be("Original description"); // Description unchanged
    }

    [Fact]
    public async Task UpdatePet_ReturnsAllRelationships()
    {
        // Arrange
        Client.AddAuthToken(_ownerToken);
        var updateDto = new UpdatePetDto { Name = "Check Relationships" };

        // Act
        var response = await Client.PatchAsJsonAsync(
            TestConstants.IntegrationTests.ApiPaths.PetById(_testPetId),
            updateDto
        );

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
        Client.AddAuthToken(_ownerToken);

        // Get original IsAdopted status
        var getResponse = await Client.GetAsync(
            TestConstants.IntegrationTests.ApiPaths.PetById(_testPetId)
        );
        var originalPet = await getResponse.ReadApiResponseDataAsync<PetResponseDto>();
        var originalIsAdopted = originalPet!.IsAdopted;

        var updateDto = new UpdatePetDto { Name = "Updated Name" };

        // Act
        var response = await Client.PatchAsJsonAsync(
            TestConstants.IntegrationTests.ApiPaths.PetById(_testPetId),
            updateDto
        );

        // Assert
        var updatedPet = await response.ReadApiResponseDataAsync<PetResponseDto>();
        updatedPet!.IsAdopted.Should().Be(originalIsAdopted);
    }
}
