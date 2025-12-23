using FluentAssertions;
using PetHub.API.Data;
using PetHub.API.DTOs.User;
using PetHub.Tests.Extensions;
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
            TestConstants.Users.Email
        );
        _client.AddAuthToken(_authToken);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task GetCurrentUser_WithValidToken_ReturnsUserProfile()
    {
        // Act
        var response = await _client.GetAsync(TestConstants.ApiPaths.UsersMe);

        // Assert
        response.ShouldBeOk();

        var apiResponse = await response.ReadApiResponseAsync<UserResponseDto>();
        apiResponse.Should().NotBeNull();
        apiResponse!.Success.Should().BeTrue();
        apiResponse.Data.Should().NotBeNull();
        apiResponse.Data!.Email.Should().Be(TestConstants.Users.Email);
        apiResponse.Data.Name.Should().Be(TestConstants.Users.Username);
    }

    [Fact]
    public async Task GetCurrentUser_WithoutToken_ReturnsUnauthorized()
    {
        // Arrange
        var clientWithoutAuth = _factory.CreateClient();

        // Act
        var response = await clientWithoutAuth.GetAsync(TestConstants.ApiPaths.UsersMe);

        // Assert
        response.ShouldBeUnauthorized();
    }

    [Fact]
    public async Task PatchCurrentUser_WithValidData_UpdatesUser()
    {
        // Arrange
        var patchDto = TestConstants.DtoBuilders.CreatePatchUserDto(
            name: TestConstants.Users.UpdatedName,
            phoneNumber: TestConstants.Users.UpdatedPhone
        );

        // Act
        var response = await _client.PatchAsJsonAsync(TestConstants.ApiPaths.UsersMe, patchDto);

        // Assert
        response.ShouldBeOk();

        var apiResponse = await response.ReadApiResponseAsync<object>();
        apiResponse.Should().NotBeNull();
        apiResponse!.Success.Should().BeTrue();
        apiResponse.Message.Should().Be("User updated successfully.");

        // Verify the update
        var getResponse = await _client.GetAsync(TestConstants.ApiPaths.UsersMe);
        var userResponse = await getResponse.ReadApiResponseAsync<UserResponseDto>();
        userResponse!.Data!.Name.Should().Be(TestConstants.Users.UpdatedName);
        userResponse.Data.PhoneNumber.Should().Be(TestConstants.Users.UpdatedPhone);
    }

    [Fact]
    public async Task PatchCurrentUser_UpdateEmail_RequiresReauth()
    {
        // Arrange
        var patchDto = TestConstants.DtoBuilders.CreatePatchUserDto(
            email: TestConstants.Users.AnotherEmail
        );

        // Act
        var response = await _client.PatchAsJsonAsync(TestConstants.ApiPaths.UsersMe, patchDto);

        // Assert
        response.ShouldBeOk();

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
        var patchDto = TestConstants.DtoBuilders.CreatePatchUserDto(
            password: TestConstants.Passwords.AnotherValidPassword
        );

        // Act
        var response = await _client.PatchAsJsonAsync(TestConstants.ApiPaths.UsersMe, patchDto);

        // Assert
        response.ShouldBeOk();

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
        var patchDto = TestConstants.DtoBuilders.CreatePatchUserDto(
            name: TestConstants.Users.Username
        );

        // Act
        var response = await clientWithoutAuth.PatchAsJsonAsync(
            TestConstants.ApiPaths.UsersMe,
            patchDto
        );

        // Assert
        response.ShouldBeUnauthorized();
    }

    [Fact]
    public async Task PatchCurrentUser_WithInvalidEmail_ReturnsBadRequest()
    {
        // Arrange
        var patchDto = TestConstants.DtoBuilders.CreatePatchUserDto(
            email: TestConstants.Users.InvalidEmail
        );

        // Act
        var response = await _client.PatchAsJsonAsync(TestConstants.ApiPaths.UsersMe, patchDto);

        // Assert
        response.ShouldBeBadRequest();
    }

    [Fact]
    public async Task DeleteCurrentUser_WithValidToken_DeletesUserAndSubsequentGetReturnsNotFound()
    {
        // Act
        var response = await _client.DeleteAsync(TestConstants.ApiPaths.UsersMe);

        // Assert
        response.ShouldBeOk();

        var apiResponse = await response.ReadApiResponseAsync<object>();
        apiResponse.Should().NotBeNull();
        apiResponse!.Success.Should().BeTrue();
        apiResponse.Message.Should().Be("User deleted successfully.");

        // Subsequent request to get current user should return NotFound
        var getResponse = await _client.GetAsync(TestConstants.ApiPaths.UsersMe);
        getResponse.ShouldBeNotFound();
    }
}
