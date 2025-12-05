using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
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
/// Integration tests for the SearchPets endpoint
/// Tests the complete HTTP request/response flow including database interactions
/// </summary>
public class SearchPetsIntegrationTests : IntegrationTestBase
{
    public SearchPetsIntegrationTests(PetHubWebApplicationFactory factory)
        : base(factory) { }

    [Fact]
    public async Task SearchPets_WithoutFilters_ReturnsAllAvailablePets()
    {
        // Arrange
        var requestUri = $"{TestConstants.ApiPaths.PetsSearch}?page=1&pageSize=10";

        // Act
        var response = await Client.GetAsync(requestUri);

        // Assert
        response.ShouldBeOk();

        var result = await response.ReadApiResponseDataAsync<PagedResult<PetResponseDto>>();
        result.Should().NotBeNull();
        result!.Items.Should().NotBeNullOrEmpty();
        result.Items.Should().HaveCount(5); // 5 available pets (excluding adopted)
        result.TotalCount.Should().Be(5);
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(10);
        result.TotalPages.Should().Be(1);
        result.HasNextPage.Should().BeFalse();
        result.HasPreviousPage.Should().BeFalse();
    }

    [Fact]
    public async Task SearchPets_WithPagination_ReturnsCorrectPage()
    {
        // Arrange
        var requestUri = $"{TestConstants.ApiPaths.PetsSearch}?page=1&pageSize=2";

        // Act
        var response = await Client.GetAsync(requestUri);

        // Assert
        response.ShouldBeOk();

        var result = await response.ReadApiResponseDataAsync<PagedResult<PetResponseDto>>();
        result.Should().NotBeNull();
        result!.Items.Should().HaveCount(2);
        result.TotalCount.Should().Be(5);
        result.PageSize.Should().Be(2);
        result.TotalPages.Should().Be(3);
        result.HasNextPage.Should().BeTrue();
        result.HasPreviousPage.Should().BeFalse();
    }

    [Fact]
    public async Task SearchPets_FilterBySpecies_ReturnsOnlyMatchingPets()
    {
        // Arrange
        var requestUri =
            $"{TestConstants.ApiPaths.PetsSearch}?species={TestConstants.SpeciesAndBreeds.DogName}";

        // Act
        var response = await Client.GetAsync(requestUri);

        // Assert
        response.ShouldBeOk();

        var result = await response.ReadApiResponseDataAsync<PagedResult<PetResponseDto>>();
        result.Should().NotBeNull();
        result!.Items.Should().NotBeNullOrEmpty();
        result.Items.Should().HaveCount(3); // Rex, Max and Bella (dogs only)
        result
            .Items.Should()
            .OnlyContain(p => p.SpeciesName == TestConstants.SpeciesAndBreeds.DogName);
        result.TotalCount.Should().Be(3);
    }

    [Fact]
    public async Task SearchPets_FilterByGender_ReturnsOnlyMatchingPets()
    {
        // Arrange
        var requestUri = $"{TestConstants.ApiPaths.PetsSearch}?gender={PetGender.Female}";

        // Act
        var response = await Client.GetAsync(requestUri);

        // Assert
        response.ShouldBeOk();

        var result = await response.ReadApiResponseDataAsync<PagedResult<PetResponseDto>>();
        result.Should().NotBeNull();
        result!.Items.Should().NotBeNullOrEmpty();
        result.Items.Should().HaveCount(3); // Luna, Mia and Bella (females only)
        result.Items.Should().OnlyContain(p => p.Gender == PetGender.Female);
        result.TotalCount.Should().Be(3);
    }

    [Fact]
    public async Task SearchPets_FilterBySize_ReturnsOnlyMatchingPets()
    {
        // Arrange
        var requestUri = $"{TestConstants.ApiPaths.PetsSearch}?size={PetSize.Large}";

        // Act
        var response = await Client.GetAsync(requestUri);

        // Assert
        response.ShouldBeOk();

        var result = await response.ReadApiResponseDataAsync<PagedResult<PetResponseDto>>();
        result.Should().NotBeNull();
        result!.Items.Should().NotBeNullOrEmpty();
        result.Items.Should().HaveCount(1); // Only Rex
        result.Items.Should().OnlyContain(p => p.Size == PetSize.Large);
        result.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task SearchPets_FilterByBreed_ReturnsOnlyMatchingPets()
    {
        // Arrange
        var requestUri =
            $"{TestConstants.ApiPaths.PetsSearch}?breed={TestConstants.SpeciesAndBreeds.LabradorName}";

        // Act
        var response = await Client.GetAsync(requestUri);

        // Assert
        response.ShouldBeOk();

        var result = await response.ReadApiResponseDataAsync<PagedResult<PetResponseDto>>();
        result.Should().NotBeNull();
        result!.Items.Should().NotBeNullOrEmpty();
        result.Items.Should().HaveCount(2); // Rex and Bella
        result
            .Items.Should()
            .OnlyContain(p => p.BreedName == TestConstants.SpeciesAndBreeds.LabradorName);
        result.TotalCount.Should().Be(2);
    }

    [Fact]
    public async Task SearchPets_FilterByColor_ReturnsOnlyMatchingPets()
    {
        // Arrange
        var requestUri =
            $"{TestConstants.ApiPaths.PetsSearch}?colors={TestConstants.Tags.WhiteName}";

        // Act
        var response = await Client.GetAsync(requestUri);

        // Assert
        response.ShouldBeOk();

        var result = await response.ReadApiResponseDataAsync<PagedResult<PetResponseDto>>();
        result.Should().NotBeNull();
        result!.Items.Should().NotBeNullOrEmpty();
        result.Items.Should().HaveCount(3); // Luna, Mia and Bella (white pets)
        result
            .Items.Should()
            .OnlyContain(p => p.Tags.Any(t => t.Name == TestConstants.Tags.WhiteName));
        result.TotalCount.Should().Be(3);
    }

    [Fact]
    public async Task SearchPets_FilterByMultipleColors_ReturnsOnlyPetsWithAllColors()
    {
        // Arrange - search for pets that have BOTH colors (White AND Black)
        var requestUri =
            $"{TestConstants.ApiPaths.PetsSearch}?colors={TestConstants.Tags.WhiteName},{TestConstants.Tags.BlackName}";

        // Act
        var response = await Client.GetAsync(requestUri);

        // Assert
        response.ShouldBeOk();

        var result = await response.ReadApiResponseDataAsync<PagedResult<PetResponseDto>>();
        result.Should().NotBeNull();
        result!.Items.Should().NotBeNullOrEmpty();
        result.Items.Should().HaveCount(1); // Only Bella (has both white AND black)
        result.Items.First().Name.Should().Be(TestConstants.Pets.Bella);
        result.Items.First().Tags.Should().Contain(t => t.Name == TestConstants.Tags.WhiteName);
        result.Items.First().Tags.Should().Contain(t => t.Name == TestConstants.Tags.BlackName);
        result.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task SearchPets_FilterByCoat_ReturnsOnlyMatchingPets()
    {
        // Arrange
        var requestUri =
            $"{TestConstants.ApiPaths.PetsSearch}?coat={TestConstants.Tags.LongCoatName}";

        // Act
        var response = await Client.GetAsync(requestUri);

        // Assert
        response.ShouldBeOk();

        var result = await response.ReadApiResponseDataAsync<PagedResult<PetResponseDto>>();
        result.Should().NotBeNull();
        result!.Items.Should().NotBeNullOrEmpty();
        result.Items.Should().HaveCount(2); // Max and Mia (long coat)
        result
            .Items.Should()
            .OnlyContain(p => p.Tags.Any(t => t.Name == TestConstants.Tags.LongCoatName));
        result.TotalCount.Should().Be(2);
    }

    [Fact]
    public async Task SearchPets_CombinedFilters_ReturnsCorrectResults()
    {
        // Arrange
        var requestUri =
            $"{TestConstants.ApiPaths.PetsSearch}?species={TestConstants.SpeciesAndBreeds.DogName}&size={PetSize.Large}";

        // Act
        var response = await Client.GetAsync(requestUri);

        // Assert
        response.ShouldBeOk();

        var result = await response.ReadApiResponseDataAsync<PagedResult<PetResponseDto>>();
        result.Should().NotBeNull();
        result!.Items.Should().NotBeNullOrEmpty();
        result.Items.Should().HaveCount(1); // Only Rex (large dog)
        result.Items.First().Name.Should().Be(TestConstants.Pets.Rex);
        result.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task SearchPets_NoMatchingResults_ReturnsEmptyList()
    {
        // Arrange
        var requestUri = $"{TestConstants.ApiPaths.PetsSearch}?species=Papagaio";

        // Act
        var response = await Client.GetAsync(requestUri);

        // Assert
        response.ShouldBeOk();

        var result = await response.ReadApiResponseDataAsync<PagedResult<PetResponseDto>>();
        result.Should().NotBeNull();
        result!.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
        result.TotalPages.Should().Be(0);
    }

    [Fact]
    public async Task SearchPets_InvalidPageNumber_ReturnsEmptyList()
    {
        // Arrange
        var requestUri = $"{TestConstants.ApiPaths.PetsSearch}?page=999&pageSize=10";

        // Act
        var response = await Client.GetAsync(requestUri);

        // Assert
        response.ShouldBeOk();

        var result = await response.ReadApiResponseDataAsync<PagedResult<PetResponseDto>>();
        result.Should().NotBeNull();
        result!.Items.Should().BeEmpty();
        result.Page.Should().Be(999);
    }

    [Fact]
    public async Task SearchPets_ResponseStructure_IsCorrect()
    {
        // Arrange
        var requestUri = $"{TestConstants.ApiPaths.PetsSearch}?page=1&pageSize=1";

        // Act
        var response = await Client.GetAsync(requestUri);

        // Assert
        response.ShouldBeOk();

        var result = await response.ReadApiResponseDataAsync<PagedResult<PetResponseDto>>();
        result.Should().NotBeNull();
        result!.Items.Should().HaveCount(1);

        var pet = result.Items.First();
        pet.Id.Should().BeGreaterThan(0);
        pet.Name.Should().NotBeNullOrEmpty();
        pet.SpeciesName.Should().NotBeNullOrEmpty();
        pet.BreedName.Should().NotBeNullOrEmpty();
        pet.Gender.Should().BeDefined();
        pet.Size.Should().BeDefined();
        pet.AgeInMonths.Should().BeGreaterThan(0);
        pet.Owner.Should().NotBeNull();
        pet.Owner.Name.Should().NotBeNullOrEmpty();
        pet.Tags.Should().NotBeNull();
    }

    [Fact]
    public async Task SearchPets_ExcludesAdoptedPets_ByDefault()
    {
        // Arrange
        var requestUri = TestConstants.ApiPaths.PetsSearch;

        // Act
        var response = await Client.GetAsync(requestUri);

        // Assert
        response.ShouldBeOk();

        var result = await response.ReadApiResponseDataAsync<PagedResult<PetResponseDto>>();
        result.Should().NotBeNull();
        result!.Items.Should().NotContain(p => p.Name == TestConstants.Pets.Thor);
        result.Items.Should().OnlyContain(p => !p.IsAdopted);
    }
}
