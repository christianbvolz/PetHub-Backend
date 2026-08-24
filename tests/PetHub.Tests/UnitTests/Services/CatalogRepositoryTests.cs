using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using PetHub.API.Data;
using PetHub.API.Enums;
using PetHub.API.Models;
using PetHub.API.Services;

namespace PetHub.Tests.UnitTests.Services;

public class CatalogRepositoryTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly CatalogRepository _repository;

    public CatalogRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new AppDbContext(options);
        _repository = new CatalogRepository(_context);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task GetSpeciesAsync_ReturnsSpeciesOrderedByName()
    {
        _context.Species.AddRange(
            new Species { Name = TestConstants.SpeciesAndBreeds.DogName },
            new Species { Name = TestConstants.SpeciesAndBreeds.CatName }
        );
        await _context.SaveChangesAsync();

        var result = await _repository.GetSpeciesAsync();

        result.Select(s => s.Name).Should().Equal(
            TestConstants.SpeciesAndBreeds.CatName,
            TestConstants.SpeciesAndBreeds.DogName
        );
    }

    [Fact]
    public async Task GetSpeciesAsync_WhenEmpty_ReturnsEmptyList()
    {
        var result = await _repository.GetSpeciesAsync();

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task SpeciesExistsAsync_WithExistingSpecies_ReturnsTrue()
    {
        var species = TestConstants.SpeciesAndBreeds.CreateDogSpecies();
        _context.Species.Add(species);
        await _context.SaveChangesAsync();

        var exists = await _repository.SpeciesExistsAsync(species.Id);

        exists.Should().BeTrue();
    }

    [Fact]
    public async Task SpeciesExistsAsync_WithNonExistingSpecies_ReturnsFalse()
    {
        var exists = await _repository.SpeciesExistsAsync(TestConstants.NonExistentIds.Generic);

        exists.Should().BeFalse();
    }

    [Fact]
    public async Task GetBreedsBySpeciesIdAsync_ReturnsOnlyBreedsForThatSpecies()
    {
        var dog = TestConstants.SpeciesAndBreeds.CreateDogSpecies();
        var cat = TestConstants.SpeciesAndBreeds.CreateCatSpecies();
        _context.Species.AddRange(dog, cat);
        _context.Breeds.AddRange(
            new Breed
            {
                Name = TestConstants.SpeciesAndBreeds.PoodleName,
                SpeciesId = dog.Id,
            },
            new Breed
            {
                Name = TestConstants.SpeciesAndBreeds.LabradorName,
                SpeciesId = dog.Id,
            },
            new Breed
            {
                Name = TestConstants.SpeciesAndBreeds.SiameseName,
                SpeciesId = cat.Id,
            }
        );
        await _context.SaveChangesAsync();

        var result = await _repository.GetBreedsBySpeciesIdAsync(dog.Id);

        result.Should().HaveCount(2);
        result.Select(b => b.Name).Should().Equal(
            TestConstants.SpeciesAndBreeds.LabradorName,
            TestConstants.SpeciesAndBreeds.PoodleName
        );
        result.Should().OnlyContain(b => b.SpeciesId == dog.Id);
    }

    [Fact]
    public async Task GetBreedsBySpeciesIdAsync_WhenSpeciesHasNoBreeds_ReturnsEmptyList()
    {
        var species = TestConstants.SpeciesAndBreeds.CreateDogSpecies();
        _context.Species.Add(species);
        await _context.SaveChangesAsync();

        var result = await _repository.GetBreedsBySpeciesIdAsync(species.Id);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetTagsAsync_WithoutCategory_ReturnsAllTagsOrderedByCategoryThenName()
    {
        _context.Tags.AddRange(
            new Tag { Name = "Golden", Category = TagCategory.Color },
            new Tag { Name = "Black", Category = TagCategory.Color },
            new Tag { Name = "Short", Category = TagCategory.Coat },
            new Tag { Name = "Spotted", Category = TagCategory.Pattern }
        );
        await _context.SaveChangesAsync();

        var result = await _repository.GetTagsAsync(category: null);

        result.Should().HaveCount(4);
        result.Select(t => t.Name).Should().Equal("Black", "Golden", "Spotted", "Short");
    }

    [Fact]
    public async Task GetTagsAsync_WithCategory_ReturnsOnlyMatchingTags()
    {
        _context.Tags.AddRange(
            new Tag { Name = TestConstants.Tags.BlackTagName, Category = TagCategory.Color },
            new Tag { Name = TestConstants.Tags.WhiteTagName, Category = TagCategory.Color },
            new Tag { Name = TestConstants.Tags.SpottedTagName, Category = TagCategory.Pattern },
            new Tag { Name = "Short", Category = TagCategory.Coat }
        );
        await _context.SaveChangesAsync();

        var result = await _repository.GetTagsAsync(TagCategory.Color);

        result.Should().HaveCount(2);
        result.Should().OnlyContain(t => t.Category == TagCategory.Color);
        result.Select(t => t.Name).Should().Equal(
            TestConstants.Tags.BlackTagName,
            TestConstants.Tags.WhiteTagName
        );
    }
}
