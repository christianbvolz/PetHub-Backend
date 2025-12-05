using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PetHub.API.DTOs.Common;
using PetHub.API.DTOs.User;
using PetHub.Tests;
using PetHub.Tests.Extensions;
using PetHub.Tests.IntegrationTests.Infrastructure;

namespace PetHub.Tests.IntegrationTests.Controllers.AuthControllerTests;

public class RefreshTokenTests : IClassFixture<PetHubWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly PetHubWebApplicationFactory _factory;

    public RefreshTokenTests(PetHubWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Login_ShouldSetRefreshTokenCookie()
    {
        // Arrange
        var registerDto = TestConstants.DtoBuilders.CreateValidUserDto(
            email: "refresh@test.com",
            password: TestConstants.Passwords.ValidPassword
        );
        await _client.PostAsJsonAsync(TestConstants.ApiPaths.AuthRegister, registerDto);

        var loginDto = TestConstants.DtoBuilders.CreateLoginDto(
            email: "refresh@test.com",
            password: TestConstants.Passwords.ValidPassword
        );

        // Act
        var response = await _client.PostAsJsonAsync(TestConstants.ApiPaths.AuthLogin, loginDto);

        // Assert
        response.ShouldBeOk();

        // Check that refresh token cookie is set
        var cookies = response.Headers.GetValues("Set-Cookie");
        cookies.Should().Contain(c => c.Contains("refreshToken="));
        cookies.Should().Contain(c => c.ToLower().Contains("httponly"));

        // Check that response body does NOT contain the refresh token
        var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<LoginResponseDto>>();
        apiResponse.Should().NotBeNull();
        apiResponse!.Data.Should().NotBeNull();
        apiResponse.Data!.RefreshToken.Should().BeNull();
    }

    [Fact]
    public async Task Refresh_WithValidToken_ShouldReturnNewAccessToken()
    {
        // Arrange: Register and login to get initial tokens
        var registerDto = TestConstants.DtoBuilders.CreateValidUserDto(
            email: "refresh2@test.com",
            password: TestConstants.Passwords.ValidPassword
        );
        await _client.PostAsJsonAsync(TestConstants.ApiPaths.AuthRegister, registerDto);

        var loginDto = TestConstants.DtoBuilders.CreateLoginDto(
            email: "refresh2@test.com",
            password: TestConstants.Passwords.ValidPassword
        );
        var loginResponse = await _client.PostAsJsonAsync(
            TestConstants.ApiPaths.AuthLogin,
            loginDto
        );
        loginResponse.ShouldBeOk();

        // Extract refresh token from cookie
        var cookies = loginResponse.Headers.GetValues("Set-Cookie").ToList();
        var refreshTokenCookie = cookies.First(c => c.Contains("refreshToken=")).Split(';')[0];
        refreshTokenCookie.Should().NotBeNullOrEmpty();

        // Act: Use refresh token to get new access token via body (cookie simulation in tests)
        var tokenValue = refreshTokenCookie.Split('=')[1];

        var refreshDto = new RefreshRequestDto { RefreshToken = tokenValue };
        var refreshResponse = await _client.PostAsJsonAsync("/api/auth/refresh", refreshDto);

        // Assert
        refreshResponse.ShouldBeOk();

        var apiResponse = await refreshResponse.Content.ReadFromJsonAsync<
            ApiResponse<LoginResponseDto>
        >();
        apiResponse.Should().NotBeNull();
        apiResponse!.Data.Should().NotBeNull();
        apiResponse.Data!.Token.Should().NotBeNullOrEmpty();
        apiResponse.Data.User.Should().NotBeNull();

        // Should set a new refresh token cookie
        var newCookies = refreshResponse.Headers.GetValues("Set-Cookie");
        newCookies.Should().Contain(c => c.Contains("refreshToken="));
    }

    [Fact]
    public async Task Refresh_WithReusedToken_ShouldRevokeAllTokensAndReturnError()
    {
        // Arrange: Register and login
        var registerDto = TestConstants.DtoBuilders.CreateValidUserDto(
            email: "refresh3@test.com",
            password: TestConstants.Passwords.ValidPassword
        );
        await _client.PostAsJsonAsync(TestConstants.ApiPaths.AuthRegister, registerDto);

        var loginDto = TestConstants.DtoBuilders.CreateLoginDto(
            email: "refresh3@test.com",
            password: TestConstants.Passwords.ValidPassword
        );
        var loginResponse = await _client.PostAsJsonAsync(
            TestConstants.ApiPaths.AuthLogin,
            loginDto
        );

        // Extract refresh token
        var cookies = loginResponse.Headers.GetValues("Set-Cookie").ToList();
        var oldRefreshToken = cookies
            .First(c => c.Contains("refreshToken="))
            .Split(';')[0]
            .Split('=')[1];

        // Act: Refresh once (this should work and rotate the token)
        var firstRefreshDto = new RefreshRequestDto { RefreshToken = oldRefreshToken };
        var firstRefreshResponse = await _client.PostAsJsonAsync(
            "/api/auth/refresh",
            firstRefreshDto
        );
        firstRefreshResponse.ShouldBeOk();

        // Act: Try to reuse the old refresh token (this should fail and revoke all tokens)
        var secondRefreshDto = new RefreshRequestDto { RefreshToken = oldRefreshToken };
        var secondRefreshResponse = await _client.PostAsJsonAsync(
            "/api/auth/refresh",
            secondRefreshDto
        );

        // Assert: Should fail with error
        await secondRefreshResponse.ShouldBeBadRequest().WithErrorMessage("invalidated");
    }

    [Fact]
    public async Task Refresh_WithInvalidToken_ShouldReturnBadRequest()
    {
        // Arrange
        var invalidToken = "invalid-token-string";

        // Act
        var refreshDto = new RefreshRequestDto { RefreshToken = invalidToken };
        var refreshResponse = await _client.PostAsJsonAsync("/api/auth/refresh", refreshDto);

        // Assert
        refreshResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Refresh_WithoutToken_ShouldReturnBadRequest()
    {
        // Act
        var refreshDto = new RefreshRequestDto { RefreshToken = null };
        var response = await _client.PostAsJsonAsync("/api/auth/refresh", refreshDto);

        // Assert
        await response.ShouldBeBadRequest().WithErrorMessage("required");
    }

    [Fact]
    public async Task Revoke_WithValidToken_ShouldRevokeTokenAndDeleteCookie()
    {
        // Arrange: Register and login
        var registerDto = TestConstants.DtoBuilders.CreateValidUserDto(
            email: "revoke@test.com",
            password: TestConstants.Passwords.ValidPassword
        );
        await _client.PostAsJsonAsync(TestConstants.ApiPaths.AuthRegister, registerDto);

        var loginDto = TestConstants.DtoBuilders.CreateLoginDto(
            email: "revoke@test.com",
            password: TestConstants.Passwords.ValidPassword
        );
        var loginResponse = await _client.PostAsJsonAsync(
            TestConstants.ApiPaths.AuthLogin,
            loginDto
        );

        // Extract refresh token
        var cookies = loginResponse.Headers.GetValues("Set-Cookie").ToList();
        var refreshToken = cookies.First(c => c.Contains("refreshToken=")).Split(';')[0].Split('=')[
            1
        ];

        // Act: Revoke the token
        var revokeDto = new RevokeRequestDto { RefreshToken = refreshToken };
        var revokeResponse = await _client.PostAsJsonAsync("/api/auth/revoke", revokeDto);

        // Assert
        revokeResponse.ShouldBeOk();

        var apiResponse = await revokeResponse.Content.ReadFromJsonAsync<ApiResponse<string>>();
        apiResponse.Should().NotBeNull();
        apiResponse!.Success.Should().BeTrue();
        apiResponse.Data.Should().Contain("revoked");

        // Cookie should be deleted
        var revokeCookies = revokeResponse.Headers.GetValues("Set-Cookie");
        revokeCookies.Should().Contain(c => c.Contains("refreshToken=") && c.Contains("expires="));

        // Act: Try to use the revoked token
        var refreshDto = new RefreshRequestDto { RefreshToken = refreshToken };
        var refreshResponse = await _client.PostAsJsonAsync("/api/auth/refresh", refreshDto);

        // Assert: Should fail
        refreshResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Revoke_WithoutToken_ShouldReturnBadRequest()
    {
        // Act
        var revokeDto = new RevokeRequestDto { RefreshToken = null };
        var response = await _client.PostAsJsonAsync("/api/auth/revoke", revokeDto);

        // Assert
        await response.ShouldBeBadRequest().WithErrorMessage("required");
    }

    [Fact]
    public async Task Revoke_WithInvalidToken_ShouldReturnBadRequest()
    {
        // Arrange
        var invalidToken = "invalid-token-that-doesnt-exist-in-database";

        // Act
        var revokeDto = new RevokeRequestDto { RefreshToken = invalidToken };
        var revokeResponse = await _client.PostAsJsonAsync("/api/auth/revoke", revokeDto);

        // Assert
        revokeResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
