using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using PetHub.API.Data;
using PetHub.API.DTOs.Common;
using PetHub.API.DTOs.User;
using PetHub.Tests.IntegrationTests.Helpers;
using PetHub.Tests.IntegrationTests.Infrastructure;

namespace PetHub.Tests.IntegrationTests.Controllers.UsersControllerTests;

/// <summary>
/// Integration tests for Users endpoints
/// </summary>
public class UsersIntegrationTests : IClassFixture<PetHubWebApplicationFactory>, IAsyncLifetime
{
    private readonly HttpClient _client;
    private readonly PetHubWebApplicationFactory _factory;
    private string _authToken = string.Empty;

    public UsersIntegrationTests(PetHubWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    public async Task InitializeAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await dbContext.Database.EnsureDeletedAsync();
        await dbContext.Database.EnsureCreatedAsync();
        await TestDataSeeder.SeedTestData(dbContext);

        _authToken = await AuthenticationHelper.RegisterAndGetTokenAsync(
            _client,
            "userstest@example.com"
        );
        _client.AddAuthToken(_authToken);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task GetCurrentUser_WithValidToken_ReturnsUserProfile()
    {
        // Act
        var response = await _client.GetAsync("/api/users/me");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var apiResponse = await response.ReadApiResponseAsync<UserResponseDto>();
        apiResponse.Should().NotBeNull();
        apiResponse!.Success.Should().BeTrue();
        apiResponse.Data.Should().NotBeNull();
        apiResponse.Data!.Email.Should().Be("userstest@example.com");
        apiResponse.Data.Name.Should().Be("Test User");
    }

    [Fact]
    public async Task GetCurrentUser_WithoutToken_ReturnsUnauthorized()
    {
        // Arrange
        var clientWithoutAuth = _factory.CreateClient();

        // Act
        var response = await clientWithoutAuth.GetAsync("/api/users/me");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task PatchCurrentUser_WithValidData_UpdatesUser()
    {
        // Arrange
        var patchDto = new PatchUserDto { Name = "Updated Name", PhoneNumber = "11988776655" };

        // Act
        var response = await _client.PatchAsJsonAsync("/api/users/me", patchDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var apiResponse = await response.ReadApiResponseAsync<object>();
        apiResponse.Should().NotBeNull();
        apiResponse!.Success.Should().BeTrue();
        apiResponse.Message.Should().Be("User updated successfully.");

        // Verify the update
        var getResponse = await _client.GetAsync("/api/users/me");
        var userResponse = await getResponse.ReadApiResponseAsync<UserResponseDto>();
        userResponse!.Data!.Name.Should().Be("Updated Name");
        userResponse.Data.PhoneNumber.Should().Be("11988776655");
    }

    [Fact]
    public async Task PatchCurrentUser_UpdateEmail_RequiresReauth()
    {
        // Arrange
        var patchDto = new PatchUserDto { Email = "newemail@example.com" };

        // Act
        var response = await _client.PatchAsJsonAsync("/api/users/me", patchDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var apiResponse = await response.ReadApiResponseAsync<Dictionary<string, object>>();
        apiResponse.Should().NotBeNull();
        apiResponse!.Success.Should().BeTrue();
        apiResponse
            .Message.Should()
            .Be("User updated successfully. Please login again with your new credentials.");

        apiResponse.Data.Should().ContainKey("requiresReauth");
    }

    [Fact]
    public async Task PatchCurrentUser_UpdatePassword_RequiresReauth()
    {
        // Arrange
        var patchDto = new PatchUserDto { Password = "newpassword123" };

        // Act
        var response = await _client.PatchAsJsonAsync("/api/users/me", patchDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var apiResponse = await response.ReadApiResponseAsync<object>();
        apiResponse.Should().NotBeNull();
        apiResponse!.Success.Should().BeTrue();
        apiResponse
            .Message.Should()
            .Be("User updated successfully. Please login again with your new credentials.");
    }

    [Fact]
    public async Task PatchCurrentUser_WithoutToken_ReturnsUnauthorized()
    {
        // Arrange
        var clientWithoutAuth = _factory.CreateClient();
        var patchDto = new PatchUserDto { Name = "Hacker Name" };

        // Act
        var response = await clientWithoutAuth.PatchAsJsonAsync("/api/users/me", patchDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task PatchCurrentUser_WithInvalidEmail_ReturnsBadRequest()
    {
        // Arrange
        var patchDto = new PatchUserDto { Email = "invalid-email" };

        // Act
        var response = await _client.PatchAsJsonAsync("/api/users/me", patchDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
