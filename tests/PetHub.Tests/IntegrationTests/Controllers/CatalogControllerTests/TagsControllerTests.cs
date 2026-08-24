using FluentAssertions;
using PetHub.API.DTOs.Catalog;
using PetHub.API.Enums;
using PetHub.Tests.Extensions;
using PetHub.Tests.IntegrationTests.Helpers;
using PetHub.Tests.IntegrationTests.Infrastructure;

namespace PetHub.Tests.IntegrationTests.Controllers.CatalogControllerTests;

public class TagsControllerTests : IntegrationTestBase
{
    public TagsControllerTests(PetHubWebApplicationFactory factory)
        : base(factory) { }

    [Fact]
    public async Task GetTags_WithoutFilter_ReturnsAllSeededTags()
    {
        var response = await Client.GetAsync(TestConstants.ApiPaths.Tags);

        response.ShouldBeOk();

        var tags = await response.ReadApiResponseDataAsync<List<TagResponseDto>>();
        tags.Should().NotBeNull();
        tags!
            .Select(t => t.Name)
            .Should()
            .Contain(
                [
                    TestConstants.Tags.WhiteName,
                    TestConstants.Tags.BlackName,
                    TestConstants.Tags.BrownName,
                    TestConstants.Tags.ShortCoatName,
                    TestConstants.Tags.LongCoatName,
                ]
            );
        tags.Should().OnlyContain(t => t.Id > 0 && !string.IsNullOrWhiteSpace(t.Name));
        tags.Select(t => t.Category).Should().BeInAscendingOrder();
    }

    [Fact]
    public async Task GetTags_WithoutAuthentication_ReturnsOk()
    {
        var clientWithoutAuth = Factory.CreateClient();

        var response = await clientWithoutAuth.GetAsync(TestConstants.ApiPaths.Tags);

        response.ShouldBeOk();
        var tags = await response.ReadApiResponseDataAsync<List<TagResponseDto>>();
        tags.Should().NotBeNull().And.NotBeEmpty();
    }

    [Fact]
    public async Task GetTags_FilterByColor_ReturnsOnlyColorTags()
    {
        var response = await Client.GetAsync(
            TestConstants.ApiPaths.TagsByCategory(nameof(TagCategory.Color))
        );

        response.ShouldBeOk();

        var tags = await response.ReadApiResponseDataAsync<List<TagResponseDto>>();
        tags.Should().NotBeNull();
        tags.Should().OnlyContain(t => t.Category == TagCategory.Color);
        tags!
            .Select(t => t.Name)
            .Should()
            .BeEquivalentTo(
                [
                    TestConstants.Tags.WhiteName,
                    TestConstants.Tags.BlackName,
                    TestConstants.Tags.BrownName,
                ]
            );
        tags.Select(t => t.Name).Should().BeInAscendingOrder();
    }

    [Fact]
    public async Task GetTags_FilterByCoat_ReturnsOnlyCoatTags()
    {
        var response = await Client.GetAsync(
            TestConstants.ApiPaths.TagsByCategory(nameof(TagCategory.Coat))
        );

        response.ShouldBeOk();

        var tags = await response.ReadApiResponseDataAsync<List<TagResponseDto>>();
        tags.Should().NotBeNull();
        tags.Should().OnlyContain(t => t.Category == TagCategory.Coat);
        tags!
            .Select(t => t.Name)
            .Should()
            .BeEquivalentTo(
                [TestConstants.Tags.ShortCoatName, TestConstants.Tags.LongCoatName]
            );
    }

    [Fact]
    public async Task GetTags_FilterByPattern_ReturnsEmptyListWhenNoneSeeded()
    {
        var response = await Client.GetAsync(
            TestConstants.ApiPaths.TagsByCategory(nameof(TagCategory.Pattern))
        );

        response.ShouldBeOk();

        var tags = await response.ReadApiResponseDataAsync<List<TagResponseDto>>();
        tags.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public async Task GetTags_WithInvalidCategory_ReturnsBadRequest()
    {
        var response = await Client.GetAsync(TestConstants.ApiPaths.TagsByCategory("Invalid"));

        response.ShouldBeBadRequest();
    }
}
