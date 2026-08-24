using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using PetHub.Tests.Extensions;
using PetHub.Tests.IntegrationTests.Infrastructure;

namespace PetHub.Tests.IntegrationTests.RateLimiting;

public class RateLimitingTests
{
    [Fact]
    public async Task PublicEndpoint_ExceedingGlobalLimit_Returns429()
    {
        using var factory = new RateLimitedWebApplicationFactory();
        using var client = factory.CreateClient();
        HttpResponseMessage? lastResponse = null;

        for (var i = 0; i < 4; i++)
        {
            lastResponse = await client.GetAsync(TestConstants.ApiPaths.Species);
        }

        lastResponse.Should().NotBeNull();
        lastResponse!.ShouldBeTooManyRequests();
        var problem = await lastResponse.Content.ReadFromJsonAsync<ProblemDetails>();
        problem.Should().NotBeNull();
        problem!.Title.Should().Be("Too many requests");
    }

    [Fact]
    public async Task AuthEndpoint_ExceedingAuthLimit_Returns429()
    {
        using var factory = new RateLimitedWebApplicationFactory();
        using var client = factory.CreateClient();
        HttpResponseMessage? lastResponse = null;

        for (var i = 0; i < 3; i++)
        {
            var dto = TestConstants.DtoBuilders.CreateValidUserDto();
            lastResponse = await client.PostAsJsonAsync(TestConstants.ApiPaths.AuthRegister, dto);
        }

        lastResponse.Should().NotBeNull();
        lastResponse!.ShouldBeTooManyRequests();
    }

    [Fact]
    public async Task Health_IsExemptFromGlobalRateLimit()
    {
        using var factory = new RateLimitedWebApplicationFactory();
        using var client = factory.CreateClient();

        for (var i = 0; i < 6; i++)
        {
            var response = await client.GetAsync(TestConstants.ApiPaths.Health);
            response.ShouldBeOk();
        }
    }
}
