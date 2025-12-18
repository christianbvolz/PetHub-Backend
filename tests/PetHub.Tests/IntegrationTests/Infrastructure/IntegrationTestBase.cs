using Microsoft.EntityFrameworkCore;
using PetHub.API.Data;
using PetHub.Tests.IntegrationTests.Helpers;

namespace PetHub.Tests.IntegrationTests.Infrastructure;

/// <summary>
/// Base class for integration tests that provides common setup and teardown logic.
/// Eliminates code duplication across integration test classes by centralizing:
/// - Database reset and seeding
/// - Authentication token management
/// - Common test data ID retrieval
/// - HttpClient and Factory management
/// </summary>
public abstract class IntegrationTestBase(PetHubWebApplicationFactory factory)
    : IClassFixture<PetHubWebApplicationFactory>,
        IAsyncLifetime
{
    protected readonly HttpClient Client = factory.CreateClient();
    protected readonly PetHubWebApplicationFactory Factory = factory;
    protected string AuthToken { get; private set; } = string.Empty;

    // Common IDs available to all integration tests
    protected int DogSpeciesId { get; private set; }
    protected int CatSpeciesId { get; private set; }
    protected int FirstBreedId { get; private set; }
    protected int SecondBreedId { get; private set; }
    protected List<int> TagIds { get; private set; } = new();

    /// <summary>
    /// Initializes test data before running tests in the class.
    /// Override this method if you need custom initialization logic.
    /// </summary>
    public virtual async Task InitializeAsync()
    {
        await ResetAndSeedDatabase();
        await LoadCommonIds();
        await AuthenticateTestUser();
    }

    /// <summary>
    /// Cleanup logic after all tests in the class have run.
    /// Override this method if you need custom cleanup logic.
    /// </summary>
    public virtual Task DisposeAsync()
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Resets the database and seeds it with test data using TestDataSeeder.
    /// </summary>
    protected async Task ResetAndSeedDatabase()
    {
        using var scope = Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await dbContext.Database.EnsureDeletedAsync();
        await dbContext.Database.EnsureCreatedAsync();
        await TestDataSeeder.SeedTestData(dbContext);
    }

    /// <summary>
    /// Loads commonly used IDs from the database for use in tests.
    /// Reduces repetitive queries across test classes.
    protected virtual async Task LoadCommonIds()
    {
        using var scope = Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Load species IDs
        var dogSpecies = await dbContext.Species.FirstOrDefaultAsync(s =>
            s.Name == TestConstants.SpeciesAndBreeds.DogName
        );
        var catSpecies = await dbContext.Species.FirstOrDefaultAsync(s =>
            s.Name == TestConstants.SpeciesAndBreeds.CatName
        );

        DogSpeciesId = dogSpecies?.Id ?? 0;
        CatSpeciesId = catSpecies?.Id ?? 0;

        // Load breed IDs
        var breeds = await dbContext.Breeds.Take(2).ToListAsync();
        FirstBreedId = breeds.Count > 0 ? breeds[0].Id : 0;
        SecondBreedId = breeds.Count > 1 ? breeds[1].Id : 0;

        // Load tag IDs
        TagIds = await dbContext.Tags.Select(t => t.Id).Take(5).ToListAsync();

        if (TagIds.Count < 2)
        {
            throw new InvalidOperationException(
                $"Test setup requires at least 2 tags in the database, but found {TagIds.Count}. "
                    + "Ensure TestDataSeeder is seeding data correctly."
            );
        }
    }

    /// <summary>
    /// Authenticates a test user and adds the auth token to the HttpClient.
    /// Override this method if you need custom authentication logic.
    /// </summary>
    protected virtual async Task AuthenticateTestUser(string? email = null)
    {
        email ??= TestConstants.Users.GenerateUniqueEmail();
        AuthToken = await AuthenticationHelper.RegisterAndGetTokenAsync(Client, email);
        Client.AddAuthToken(AuthToken);
    }

    /// <summary>
    /// Gets access to the database context for custom operations in derived test classes.
    /// </summary>
    protected AppDbContext GetDbContext()
    {
        var scope = Factory.Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<AppDbContext>();
    }

    /// <summary>
    /// Executes an action within a database context scope and properly disposes it.
    /// Useful for custom data setup or verification in tests.
    /// </summary>
    protected async Task WithDbContextAsync(Func<AppDbContext, Task> action)
    {
        using var scope = Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await action(dbContext);
    }
}
