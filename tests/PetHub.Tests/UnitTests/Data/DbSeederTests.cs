using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using PetHub.API.Data;

namespace PetHub.Tests.UnitTests.Data;

public class DbSeederTests : IDisposable
{
    private readonly AppDbContext _context;

    public DbSeederTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new AppDbContext(options);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task SeedCatalogAsync_EmptyDatabase_AddsSpeciesBreedsAndTags()
    {
        await DbSeeder.SeedCatalogAsync(_context);

        (await _context.Species.CountAsync()).Should().BeGreaterThan(0);
        (await _context.Breeds.CountAsync()).Should().BeGreaterThan(0);
        (await _context.Tags.CountAsync()).Should().BeGreaterThan(0);
        (await _context.Users.CountAsync()).Should().Be(0);
        (await _context.Pets.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task SeedCatalogAsync_WhenAlreadySeeded_DoesNotDuplicate()
    {
        await DbSeeder.SeedCatalogAsync(_context);
        var speciesCount = await _context.Species.CountAsync();
        var breedCount = await _context.Breeds.CountAsync();
        var tagCount = await _context.Tags.CountAsync();

        await DbSeeder.SeedCatalogAsync(_context);

        (await _context.Species.CountAsync()).Should().Be(speciesCount);
        (await _context.Breeds.CountAsync()).Should().Be(breedCount);
        (await _context.Tags.CountAsync()).Should().Be(tagCount);
    }

    [Fact]
    public async Task SeedDemoDataAsync_AfterCatalog_AddsUsersAndPets()
    {
        await DbSeeder.SeedCatalogAsync(_context);

        await DbSeeder.SeedDemoDataAsync(_context);

        (await _context.Users.CountAsync()).Should().BeGreaterThan(0);
        (await _context.Pets.CountAsync()).Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task SeedDemoDataAsync_WhenUsersExist_DoesNotAddMore()
    {
        await DbSeeder.SeedCatalogAsync(_context);
        await DbSeeder.SeedDemoDataAsync(_context);
        var userCount = await _context.Users.CountAsync();
        var petCount = await _context.Pets.CountAsync();

        await DbSeeder.SeedDemoDataAsync(_context);

        (await _context.Users.CountAsync()).Should().Be(userCount);
        (await _context.Pets.CountAsync()).Should().Be(petCount);
    }
}
