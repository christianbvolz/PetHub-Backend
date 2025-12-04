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
        var species = TestConstants.SpeciesAndBreeds.CreateDogSpecies();
        _context.Species.Add(species);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.ValidateSpeciesExistsAsync(
            TestConstants.SpeciesAndBreeds.DogSpeciesId
        );

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateSpeciesExistsAsync_WithNonExistingSpecies_ReturnsFalse()
    {
        // Act
        var result = await _repository.ValidateSpeciesExistsAsync(
            TestConstants.SpeciesAndBreeds.NonExistentSpeciesId
        );

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region ValidateBreedBelongsToSpeciesAsync Tests

    [Fact]
    public async Task ValidateBreedBelongsToSpeciesAsync_WithMatchingBreedAndSpecies_ReturnsTrue()
    {
        // Arrange
        var species = TestConstants.SpeciesAndBreeds.CreateDogSpecies();
        var breed = TestConstants.SpeciesAndBreeds.CreateLabradorBreed();
        _context.Species.Add(species);
        _context.Breeds.Add(breed);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.ValidateBreedBelongsToSpeciesAsync(
            breedId: TestConstants.SpeciesAndBreeds.LabradorBreedId,
            speciesId: TestConstants.SpeciesAndBreeds.DogSpeciesId
        );

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateBreedBelongsToSpeciesAsync_WithBreedFromDifferentSpecies_ReturnsFalse()
    {
        // Arrange
        var dogSpecies = TestConstants.SpeciesAndBreeds.CreateDogSpecies();
        var catSpecies = TestConstants.SpeciesAndBreeds.CreateCatSpecies();
        var labrador = TestConstants.SpeciesAndBreeds.CreateLabradorBreed(); // Dog breed
        _context.Species.AddRange(dogSpecies, catSpecies);
        _context.Breeds.Add(labrador);
        await _context.SaveChangesAsync();

        // Act - Try to associate dog breed with cat species
        var result = await _repository.ValidateBreedBelongsToSpeciesAsync(
            breedId: TestConstants.SpeciesAndBreeds.LabradorBreedId,
            speciesId: TestConstants.SpeciesAndBreeds.CatSpeciesId
        );

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task ValidateBreedBelongsToSpeciesAsync_WithNonExistingBreed_ReturnsFalse()
    {
        // Arrange
        var species = TestConstants.SpeciesAndBreeds.CreateDogSpecies();
        _context.Species.Add(species);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.ValidateBreedBelongsToSpeciesAsync(
            breedId: TestConstants.SpeciesAndBreeds.NonExistentBreedId,
            speciesId: TestConstants.SpeciesAndBreeds.DogSpeciesId
        );

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task ValidateBreedBelongsToSpeciesAsync_WithNonExistingSpecies_ReturnsFalse()
    {
        // Arrange
        var breed = TestConstants.SpeciesAndBreeds.CreateLabradorBreed();
        _context.Breeds.Add(breed);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.ValidateBreedBelongsToSpeciesAsync(
            breedId: TestConstants.SpeciesAndBreeds.LabradorBreedId,
            speciesId: TestConstants.SpeciesAndBreeds.NonExistentSpeciesId
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
        var tags = TestConstants.Tags.CreateAllTags();
        _context.Tags.AddRange(tags);
        await _context.SaveChangesAsync();

        // Act
        var invalidTagIds = await _repository.ValidateTagsExistAsync([
            TestConstants.Tags.BlackTagId,
            TestConstants.Tags.WhiteTagId,
            TestConstants.Tags.SpottedTagId,
        ]);

        // Assert
        invalidTagIds.Should().BeEmpty();
    }

    [Fact]
    public async Task ValidateTagsExistAsync_WithSomeNonExistingTags_ReturnsInvalidIds()
    {
        // Arrange
        var tag1 = TestConstants.Tags.CreateBlackTag();
        var tag2 = TestConstants.Tags.CreateWhiteTag();
        _context.Tags.AddRange(tag1, tag2);
        await _context.SaveChangesAsync();

        // Act
        var invalidTagIds = await _repository.ValidateTagsExistAsync([
            TestConstants.Tags.BlackTagId,
            TestConstants.Tags.WhiteTagId,
            TestConstants.Tags.NonExistentTagId1,
            TestConstants.Tags.NonExistentTagId2,
        ]);

        // Assert
        invalidTagIds.Should().HaveCount(2);
        invalidTagIds.Should().Contain(TestConstants.Tags.NonExistentTagId1);
        invalidTagIds.Should().Contain(TestConstants.Tags.NonExistentTagId2);
    }

    [Fact]
    public async Task ValidateTagsExistAsync_WithAllNonExistingTags_ReturnsAllIds()
    {
        // Act
        var invalidTagIds = await _repository.ValidateTagsExistAsync([
            TestConstants.Tags.NonExistentTagId1,
            TestConstants.Tags.NonExistentTagId2,
            TestConstants.Tags.NonExistentTagId3,
        ]);

        // Assert
        invalidTagIds.Should().HaveCount(3);
        invalidTagIds.Should().Contain(TestConstants.Tags.NonExistentTagId1);
        invalidTagIds.Should().Contain(TestConstants.Tags.NonExistentTagId2);
        invalidTagIds.Should().Contain(TestConstants.Tags.NonExistentTagId3);
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
        var tag = TestConstants.Tags.CreateBlackTag();
        _context.Tags.Add(tag);
        await _context.SaveChangesAsync();

        // Act - Pass duplicate valid and invalid IDs
        var invalidTagIds = await _repository.ValidateTagsExistAsync([
            TestConstants.Tags.BlackTagId,
            TestConstants.Tags.BlackTagId,
            TestConstants.Tags.NonExistentTagId1,
            TestConstants.Tags.NonExistentTagId1,
        ]);

        // Assert - Method should return deduplicated invalid IDs
        invalidTagIds.Should().HaveCount(1);
        invalidTagIds.Should().OnlyContain(id => id == TestConstants.Tags.NonExistentTagId1);
    }

    #endregion
}
