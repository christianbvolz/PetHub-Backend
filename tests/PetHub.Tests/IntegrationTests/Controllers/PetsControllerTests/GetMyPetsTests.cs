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
/// Integration tests for the GetMyPets endpoint (GET /api/pets/me)
/// Tests retrieving authenticated user's pets
/// </summary>
public class GetMyPetsTests : IntegrationTestBase
{
    private string _userToken = string.Empty;
    private string _otherUserToken = string.Empty;
    private readonly List<int> _userPetIds = new();

    public GetMyPetsTests(PetHubWebApplicationFactory factory)
        : base(factory) { }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();

        // Register user and create multiple test pets
        _userToken = AuthToken;

        // Create 3 pets for the user
        for (int petIndex = 1; petIndex <= 3; petIndex++)
        {
            var createDto = TestConstants.DtoBuilders.CreateValidPetDto(
                speciesId: DogSpeciesId,
                breedId: FirstBreedId,
                name: $"My Pet {petIndex}"
            );
            createDto.Gender = PetGender.Male;
            createDto.Size = PetSize.Medium;
            createDto.AgeInMonths = 12 * petIndex;

            var response = await Client.PostAsJsonAsync(
                TestConstants.IntegrationTests.ApiPaths.Pets,
                createDto
            );
            var createdPet = await response.ReadApiResponseDataAsync<PetResponseDto>();
            _userPetIds.Add(createdPet!.Id);
        }

        // Register another user and create a pet for them
        _otherUserToken = await AuthenticationHelper.RegisterAndGetTokenAsync(
            Client,
            "otherpetsuser@example.com"
        );
        Client.AddAuthToken(_otherUserToken);

        var otherPetDto = TestConstants.DtoBuilders.CreateValidPetDto(
            speciesId: DogSpeciesId,
            breedId: FirstBreedId,
            name: "Other User Pet"
        );
        otherPetDto.Gender = PetGender.Female;
        otherPetDto.Size = PetSize.Small;
        otherPetDto.AgeInMonths = 6;

        await Client.PostAsJsonAsync(TestConstants.IntegrationTests.ApiPaths.Pets, otherPetDto);
    }

    public override Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task GetMyPets_ReturnsOk()
    {
        // Arrange
        Client.AddAuthToken(_userToken);

        // Act
        var response = await Client.GetAsync(TestConstants.IntegrationTests.ApiPaths.PetsMe);

        // Assert
        response.ShouldBeOk();
    }

    [Fact]
    public async Task GetMyPets_ReturnsOnlyUsersPets()
    {
        // Arrange
        Client.AddAuthToken(_userToken);

        // Act
        var response = await Client.GetAsync(TestConstants.IntegrationTests.ApiPaths.PetsMe);

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
            Client,
            "nopetsuser@example.com"
        );
        Client.AddAuthToken(newUserToken);

        // Act
        var response = await Client.GetAsync(TestConstants.IntegrationTests.ApiPaths.PetsMe);

        // Assert
        var pets = await response.ReadApiResponseDataAsync<List<PetResponseDto>>();
        pets.Should().NotBeNull();
        pets!.Should().BeEmpty();
    }

    [Fact]
    public async Task GetMyPets_DoesNotReturnOtherUsersPets()
    {
        // Arrange
        Client.AddAuthToken(_userToken);

        // Act
        var response = await Client.GetAsync(TestConstants.IntegrationTests.ApiPaths.PetsMe);

        // Assert
        var pets = await response.ReadApiResponseDataAsync<List<PetResponseDto>>();
        pets.Should().NotBeNull();
        pets!.Should().NotContain(p => p.Name == "Other User Pet");
    }

    [Fact]
    public async Task GetMyPets_WithoutAuthentication_ReturnsUnauthorized()
    {
        // Arrange
        var clientWithoutAuth = Factory.CreateClient();

        // Act
        var response = await clientWithoutAuth.GetAsync(
            TestConstants.IntegrationTests.ApiPaths.PetsMe
        );

        // Assert
        response.ShouldBeUnauthorized();
    }

    [Fact]
    public async Task GetMyPets_ReturnsCompleteData()
    {
        // Arrange
        Client.AddAuthToken(_userToken);

        // Act
        var response = await Client.GetAsync(TestConstants.IntegrationTests.ApiPaths.PetsMe);

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
        Client.AddAuthToken(_userToken);

        // Act
        var response = await Client.GetAsync(TestConstants.IntegrationTests.ApiPaths.PetsMe);

        // Assert
        var pets = await response.ReadApiResponseDataAsync<List<PetResponseDto>>();
        pets.Should().NotBeNull();
        pets!.Should().HaveCount(3);

        // Verify descending order (newest first)
        for (int index = 0; index < pets!.Count - 1; index++)
        {
            pets[index].CreatedAt.Should().BeOnOrAfter(pets[index + 1].CreatedAt);
        }
    }

    [Fact]
    public async Task GetMyPets_IncludesAdoptedPets()
    {
        // Arrange
        Client.AddAuthToken(_userToken);

        // Mark one pet as adopted
        using var scope = Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var pet = await dbContext.Pets.FindAsync(_userPetIds[0]);
        pet!.IsAdopted = true;
        await dbContext.SaveChangesAsync();

        // Act
        var response = await Client.GetAsync(TestConstants.IntegrationTests.ApiPaths.PetsMe);

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
        Client.AddAuthToken(_userToken);

        // Delete one pet
        await Client.DeleteAsync(TestConstants.IntegrationTests.ApiPaths.PetById(_userPetIds[0]));

        // Act
        var response = await Client.GetAsync(TestConstants.IntegrationTests.ApiPaths.PetsMe);

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
        Client.AddAuthToken(_userToken);
        var userResponse = await Client.GetAsync(TestConstants.IntegrationTests.ApiPaths.PetsMe);
        var userPets = await userResponse.ReadApiResponseDataAsync<List<PetResponseDto>>();

        Client.AddAuthToken(_otherUserToken);
        var otherResponse = await Client.GetAsync(TestConstants.IntegrationTests.ApiPaths.PetsMe);
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
        Client.AddAuthToken(_userToken);

        // Act
        var response = await Client.GetAsync(TestConstants.IntegrationTests.ApiPaths.PetsMe);

        // Assert
        var apiResponse = await response.ReadApiResponseAsync<List<PetResponseDto>>();
        apiResponse.Should().NotBeNull();
        apiResponse!.Success.Should().BeTrue();
        apiResponse.Data.Should().NotBeNull();
        apiResponse.Data!.Should().HaveCount(3);
    }
}
