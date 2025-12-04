using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using PetHub.API.Data;
using PetHub.API.Enums;
using PetHub.API.Models;
using PetHub.API.Services;

namespace PetHub.Tests.UnitTests.Services;

public class PetRepositoryValidationsTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly PetRepository _repository;

    public PetRepositoryValidationsTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new AppDbContext(options);
        _repository = new PetRepository(_context);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
        GC.SuppressFinalize(this);
    }

    #region ValidateSpeciesExistsAsync Tests

    [Fact]
    public async Task ValidateSpeciesExistsAsync_WithExistingSpecies_ReturnsTrue()
    {
        // Arrange
        var species = new Species { Id = 1, Name = "Dog" };
        _context.Species.Add(species);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.ValidateSpeciesExistsAsync(1);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateSpeciesExistsAsync_WithNonExistingSpecies_ReturnsFalse()
    {
        // Act
        var result = await _repository.ValidateSpeciesExistsAsync(999);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region ValidateBreedBelongsToSpeciesAsync Tests

    [Fact]
    public async Task ValidateBreedBelongsToSpeciesAsync_WithMatchingBreedAndSpecies_ReturnsTrue()
    {
        // Arrange
        var species = new Species { Id = 1, Name = "Dog" };
        var breed = new Breed
        {
            Id = 1,
            Name = "Labrador",
            SpeciesId = 1,
        };
        _context.Species.Add(species);
        _context.Breeds.Add(breed);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.ValidateBreedBelongsToSpeciesAsync(breedId: 1, speciesId: 1);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateBreedBelongsToSpeciesAsync_WithBreedFromDifferentSpecies_ReturnsFalse()
    {
        // Arrange
        var dogSpecies = new Species { Id = 1, Name = "Dog" };
        var catSpecies = new Species { Id = 2, Name = "Cat" };
        var labrador = new Breed
        {
            Id = 1,
            Name = "Labrador",
            SpeciesId = 1,
        }; // Dog breed
        _context.Species.AddRange(dogSpecies, catSpecies);
        _context.Breeds.Add(labrador);
        await _context.SaveChangesAsync();

        // Act - Try to associate dog breed with cat species
        var result = await _repository.ValidateBreedBelongsToSpeciesAsync(breedId: 1, speciesId: 2);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task ValidateBreedBelongsToSpeciesAsync_WithNonExistingBreed_ReturnsFalse()
    {
        // Arrange
        var species = new Species { Id = 1, Name = "Dog" };
        _context.Species.Add(species);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.ValidateBreedBelongsToSpeciesAsync(
            breedId: 999,
            speciesId: 1
        );

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task ValidateBreedBelongsToSpeciesAsync_WithNonExistingSpecies_ReturnsFalse()
    {
        // Arrange
        var breed = new Breed
        {
            Id = 1,
            Name = "Labrador",
            SpeciesId = 1,
        };
        _context.Breeds.Add(breed);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.ValidateBreedBelongsToSpeciesAsync(
            breedId: 1,
            speciesId: 999
        );

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region ValidateTagsExistAsync Tests

    [Fact]
    public async Task ValidateTagsExistAsync_WithAllExistingTags_ReturnsEmptyList()
    {
        // Arrange
        var tags = new List<Tag>
        {
            new()
            {
                Id = 1,
                Name = "Black",
                Category = TagCategory.Color,
            },
            new()
            {
                Id = 2,
                Name = "White",
                Category = TagCategory.Color,
            },
            new()
            {
                Id = 3,
                Name = "Spotted",
                Category = TagCategory.Pattern,
            },
        };
        _context.Tags.AddRange(tags);
        await _context.SaveChangesAsync();

        // Act
        var invalidTagIds = await _repository.ValidateTagsExistAsync([1, 2, 3]);

        // Assert
        invalidTagIds.Should().BeEmpty();
    }

    [Fact]
    public async Task ValidateTagsExistAsync_WithSomeNonExistingTags_ReturnsInvalidIds()
    {
        // Arrange
        var tag1 = new Tag
        {
            Id = 1,
            Name = "Black",
            Category = TagCategory.Color,
        };
        var tag2 = new Tag
        {
            Id = 2,
            Name = "White",
            Category = TagCategory.Color,
        };
        _context.Tags.AddRange(tag1, tag2);
        await _context.SaveChangesAsync();

        // Act
        var invalidTagIds = await _repository.ValidateTagsExistAsync([1, 2, 999, 888]);

        // Assert
        invalidTagIds.Should().HaveCount(2);
        invalidTagIds.Should().Contain(999);
        invalidTagIds.Should().Contain(888);
    }

    [Fact]
    public async Task ValidateTagsExistAsync_WithAllNonExistingTags_ReturnsAllIds()
    {
        // Act
        var invalidTagIds = await _repository.ValidateTagsExistAsync([999, 888, 777]);

        // Assert
        invalidTagIds.Should().HaveCount(3);
        invalidTagIds.Should().Contain(999);
        invalidTagIds.Should().Contain(888);
        invalidTagIds.Should().Contain(777);
    }

    [Fact]
    public async Task ValidateTagsExistAsync_WithEmptyList_ReturnsEmptyList()
    {
        // Act
        var invalidTagIds = await _repository.ValidateTagsExistAsync([]);

        // Assert
        invalidTagIds.Should().BeEmpty();
    }

    [Fact]
    public async Task ValidateTagsExistAsync_WithDuplicateIds_ReturnsCorrectInvalidIds()
    {
        // Arrange
        var tag = new Tag
        {
            Id = 1,
            Name = "Black",
            Category = TagCategory.Color,
        };
        _context.Tags.Add(tag);
        await _context.SaveChangesAsync();

        // Act - Pass duplicate valid and invalid IDs
        var invalidTagIds = await _repository.ValidateTagsExistAsync([1, 1, 999, 999]);

        // Assert - Method should return deduplicated invalid IDs
        invalidTagIds.Should().HaveCount(1);
        invalidTagIds.Should().OnlyContain(id => id == 999);
    }

    #endregion
}
