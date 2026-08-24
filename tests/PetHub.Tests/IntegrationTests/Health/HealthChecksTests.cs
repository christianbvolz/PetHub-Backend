using FluentAssertions;
using PetHub.Tests.Extensions;
using PetHub.Tests.IntegrationTests.Infrastructure;

namespace PetHub.Tests.IntegrationTests.Health;

public class HealthChecksTests : IClassFixture<PetHubWebApplicationFactory>
{
    private readonly HttpClient _client;

    public HealthChecksTests(PetHubWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Health_ReturnsHealthyWithoutAuthentication()
    {
        var response = await _client.GetAsync(TestConstants.ApiPaths.Health);

        response.ShouldBeOk();
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Be("Healthy");
    }
}
