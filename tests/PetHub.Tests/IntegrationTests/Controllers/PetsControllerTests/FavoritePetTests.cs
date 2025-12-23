using FluentAssertions;
using PetHub.API.DTOs.Pet;
using PetHub.Tests.Extensions;
using PetHub.Tests.IntegrationTests.Helpers;
using PetHub.Tests.IntegrationTests.Infrastructure;

namespace PetHub.Tests.IntegrationTests.Controllers.PetsControllerTests;

public class FavoritePetTests : IClassFixture<PetHubWebApplicationFactory>, IAsyncLifetime
{
    private readonly PetHubWebApplicationFactory _factory;
    private readonly HttpClient _client;
    private string _authToken = string.Empty;

    public FavoritePetTests(PetHubWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    public async Task InitializeAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PetHub.API.Data.AppDbContext>();
        await dbContext.Database.EnsureDeletedAsync();
        await dbContext.Database.EnsureCreatedAsync();
        await TestDataSeeder.SeedTestData(dbContext);

        _authToken = await AuthenticationHelper.RegisterAndGetTokenAsync(_client);
        _client.AddAuthToken(_authToken);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task AddFavorite_AddsFavoriteAndAppearsInFavoritesList()
    {
        // Get a pet to favorite
        var searchResp = await _client.GetAsync(TestConstants.ApiPaths.PetsSearch);
        searchResp.ShouldBeOk();
        var paged =
            await searchResp.ReadApiResponseAsync<PetHub.API.DTOs.Common.PagedResult<PetResponseDto>>();
        paged!.Data!.Items.Should().NotBeNullOrEmpty();
        var petId = paged.Data.Items.First().Id;

        // Add favorite
        var resp = await _client.PostAsync(TestConstants.ApiPaths.PetFavorite(petId), null);
        resp.ShouldBeOk();

        // Get favorites
        var favResp = await _client.GetAsync(TestConstants.ApiPaths.PetsFavorites);
        favResp.ShouldBeOk();
        var favPaged = await favResp.ReadApiResponseAsync<List<PetResponseDto>>();
        var items = favPaged?.Data ?? throw new InvalidOperationException("Favorites data is null");
        items.Any(p => p.Id == petId).Should().BeTrue();
    }

    [Fact]
    public async Task AddFavorite_IsIdempotent()
    {
        // Get a pet to favorite
        var searchResp = await _client.GetAsync(TestConstants.ApiPaths.PetsSearch);
        searchResp.ShouldBeOk();
        var paged =
            await searchResp.ReadApiResponseAsync<PetHub.API.DTOs.Common.PagedResult<PetResponseDto>>();
        paged!.Data!.Items.Should().NotBeNullOrEmpty();
        var petId = paged.Data.Items.First().Id;

        // Add favorite twice
        var resp1 = await _client.PostAsync(TestConstants.ApiPaths.PetFavorite(petId), null);
        resp1.ShouldBeOk();
        var resp2 = await _client.PostAsync(TestConstants.ApiPaths.PetFavorite(petId), null);
        resp2.ShouldBeOk();

        // Get favorites and ensure only one entry exists for the pet
        var favResp = await _client.GetAsync(TestConstants.ApiPaths.PetsFavorites);
        favResp.ShouldBeOk();
        var favPaged = await favResp.ReadApiResponseAsync<List<PetResponseDto>>();
        var items = favPaged?.Data ?? new List<PetResponseDto>();
        items.Count(p => p.Id == petId).Should().Be(1);
    }

    [Fact]
    public async Task RemoveFavorite_RemovesFavoriteAndNoLongerAppears()
    {
        // Get a pet to favorite
        var searchResp = await _client.GetAsync(TestConstants.ApiPaths.PetsSearch);
        searchResp.ShouldBeOk();
        var paged =
            await searchResp.ReadApiResponseAsync<PetHub.API.DTOs.Common.PagedResult<PetResponseDto>>();
        paged!.Data!.Items.Should().NotBeNullOrEmpty();
        var petId = paged.Data.Items.First().Id;

        // Add favorite
        var resp = await _client.PostAsync(TestConstants.ApiPaths.PetFavorite(petId), null);
        resp.ShouldBeOk();

        // Remove favorite
        var del = await _client.DeleteAsync(TestConstants.ApiPaths.PetFavorite(petId));
        del.ShouldBeOk();

        // Get favorites
        var favResp = await _client.GetAsync(TestConstants.ApiPaths.PetsFavorites);
        favResp.ShouldBeOk();
        var favPaged = await favResp.ReadApiResponseAsync<List<PetResponseDto>>();
        var items = favPaged?.Data ?? new List<PetResponseDto>();
        items.Any(p => p.Id == petId).Should().BeFalse();
    }
}
