using FluentAssertions;
using PetHub.API.DTOs.Catalog;
using PetHub.Tests.Extensions;
using PetHub.Tests.IntegrationTests.Helpers;
using PetHub.Tests.IntegrationTests.Infrastructure;

namespace PetHub.Tests.IntegrationTests.Controllers.CatalogControllerTests;

public class SpeciesControllerTests : IntegrationTestBase
{
    public SpeciesControllerTests(PetHubWebApplicationFactory factory)
        : base(factory) { }

    [Fact]
    public async Task GetSpecies_ReturnsOkWithSeededSpecies()
    {
        var response = await Client.GetAsync(TestConstants.ApiPaths.Species);

        response.ShouldBeOk();

        var species = await response.ReadApiResponseDataAsync<List<SpeciesResponseDto>>();
        species.Should().NotBeNull();
        species!
            .Select(s => s.Name)
            .Should()
            .Contain(
                [
                    TestConstants.SpeciesAndBreeds.DogName,
                    TestConstants.SpeciesAndBreeds.CatName,
                ]
            );
        species.Should().OnlyContain(s => s.Id > 0 && !string.IsNullOrWhiteSpace(s.Name));
        species.Select(s => s.Name).Should().BeInAscendingOrder();
    }

    [Fact]
    public async Task GetSpecies_WithoutAuthentication_ReturnsOk()
    {
        var clientWithoutAuth = Factory.CreateClient();

        var response = await clientWithoutAuth.GetAsync(TestConstants.ApiPaths.Species);

        response.ShouldBeOk();
        var species = await response.ReadApiResponseDataAsync<List<SpeciesResponseDto>>();
        species.Should().NotBeNull().And.NotBeEmpty();
    }

    [Fact]
    public async Task GetBreedsBySpecies_WithValidDogSpecies_ReturnsOnlyDogBreeds()
    {
        var response = await Client.GetAsync(TestConstants.ApiPaths.SpeciesBreeds(DogSpeciesId));

        response.ShouldBeOk();

        var breeds = await response.ReadApiResponseDataAsync<List<BreedResponseDto>>();
        breeds.Should().NotBeNull();
        breeds!
            .Select(b => b.Name)
            .Should()
            .Contain(
                [
                    TestConstants.SpeciesAndBreeds.LabradorName,
                    TestConstants.SpeciesAndBreeds.PoodleName,
                ]
            );
        breeds.Select(b => b.Name).Should().NotContain(TestConstants.SpeciesAndBreeds.SiameseName);
        breeds.Should().OnlyContain(b => b.SpeciesId == DogSpeciesId && b.Id > 0);
        breeds.Select(b => b.Name).Should().BeInAscendingOrder();
    }

    [Fact]
    public async Task GetBreedsBySpecies_WithValidCatSpecies_ReturnsOnlyCatBreeds()
    {
        var response = await Client.GetAsync(TestConstants.ApiPaths.SpeciesBreeds(CatSpeciesId));

        response.ShouldBeOk();

        var breeds = await response.ReadApiResponseDataAsync<List<BreedResponseDto>>();
        breeds.Should().NotBeNull();
        breeds!
            .Select(b => b.Name)
            .Should()
            .BeEquivalentTo(
                [
                    TestConstants.SpeciesAndBreeds.PersianName,
                    TestConstants.SpeciesAndBreeds.SiameseName,
                ]
            );
        breeds.Should().OnlyContain(b => b.SpeciesId == CatSpeciesId);
    }

    [Fact]
    public async Task GetBreedsBySpecies_WithNonExistentSpecies_ReturnsNotFound()
    {
        var response = await Client.GetAsync(
            TestConstants.ApiPaths.SpeciesBreeds(TestConstants.NonExistentIds.Generic)
        );

        response.ShouldBeNotFound();
        await response.WithErrorMessage(
            $"Species with ID {TestConstants.NonExistentIds.Generic} not found."
        );
    }

    [Fact]
    public async Task GetBreedsBySpecies_WithoutAuthentication_ReturnsOk()
    {
        var clientWithoutAuth = Factory.CreateClient();

        var response = await clientWithoutAuth.GetAsync(
            TestConstants.ApiPaths.SpeciesBreeds(DogSpeciesId)
        );

        response.ShouldBeOk();
        var breeds = await response.ReadApiResponseDataAsync<List<BreedResponseDto>>();
        breeds.Should().NotBeNull().And.NotBeEmpty();
    }
}
