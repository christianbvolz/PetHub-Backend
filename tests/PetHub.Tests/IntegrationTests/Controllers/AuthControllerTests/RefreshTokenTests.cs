using System.Net;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using PetHub.API.Data;
using PetHub.API.DTOs.Common;
using PetHub.API.DTOs.User;
using PetHub.API.Utils;
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
        var email = TestConstants.Users.GenerateUniqueEmail();
        var registerDto = TestConstants.DtoBuilders.CreateValidUserDto(
            email: email,
            password: TestConstants.Passwords.ValidPassword
        );
        await _client.PostAsJsonAsync(TestConstants.ApiPaths.AuthRegister, registerDto);

        var loginDto = TestConstants.DtoBuilders.CreateLoginDto(
            email: email,
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
        var email = TestConstants.Users.GenerateUniqueEmail();
        var registerDto = TestConstants.DtoBuilders.CreateValidUserDto(
            email: email,
            password: TestConstants.Passwords.ValidPassword
        );
        await _client.PostAsJsonAsync(TestConstants.ApiPaths.AuthRegister, registerDto);

        var loginDto = TestConstants.DtoBuilders.CreateLoginDto(
            email: email,
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
        var refreshResponse = await _client.PostAsJsonAsync(
            TestConstants.ApiPaths.AuthRefresh,
            refreshDto
        );

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
        var email = TestConstants.Users.GenerateUniqueEmail();
        var registerDto = TestConstants.DtoBuilders.CreateValidUserDto(
            email: email,
            password: TestConstants.Passwords.ValidPassword
        );
        await _client.PostAsJsonAsync(TestConstants.ApiPaths.AuthRegister, registerDto);

        var loginDto = TestConstants.DtoBuilders.CreateLoginDto(
            email: email,
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
            TestConstants.ApiPaths.AuthRefresh,
            firstRefreshDto
        );
        firstRefreshResponse.ShouldBeOk();

        // Act: Try to reuse the old refresh token (this should fail and revoke all tokens)
        var secondRefreshDto = new RefreshRequestDto { RefreshToken = oldRefreshToken };
        var secondRefreshResponse = await _client.PostAsJsonAsync(
            TestConstants.ApiPaths.AuthRefresh,
            secondRefreshDto
        );

        // Assert: Should fail with BadRequest (implementation uses batch update)
        secondRefreshResponse.ShouldBeBadRequest();

        // Verify in DB that the original token was revoked
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var user = await db.Users.FirstAsync(u => u.Email == email);
        var oldTokenHash = RefreshTokenHelper.ComputeSha256Hash(oldRefreshToken);
        var oldToken = await db.RefreshTokens.FirstAsync(t => t.TokenHash == oldTokenHash);

        oldToken.RevokedAt.Should().NotBeNull("the reused token should be revoked");

        // Ensure at least one token was revoked for this user
        var revoked = await db
            .RefreshTokens.Where(t => t.UserId == user.Id && t.RevokedAt != null)
            .ToListAsync();
        revoked.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Refresh_WithInvalidToken_ShouldReturnBadRequest()
    {
        // Arrange
        var invalidToken = "invalid-token-string";

        // Act
        var refreshDto = new RefreshRequestDto { RefreshToken = invalidToken };
        var refreshResponse = await _client.PostAsJsonAsync(
            TestConstants.ApiPaths.AuthRefresh,
            refreshDto
        );

        // Assert
        refreshResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Refresh_WithoutToken_ShouldReturnBadRequest()
    {
        // Act
        var refreshDto = new RefreshRequestDto { RefreshToken = null };
        var response = await _client.PostAsJsonAsync(
            TestConstants.ApiPaths.AuthRefresh,
            refreshDto
        );

        // Assert
        await response.ShouldBeBadRequest().WithErrorMessage("required");
    }

    [Fact]
    public async Task Revoke_WithValidToken_ShouldRevokeTokenAndDeleteCookie()
    {
        // Arrange: Register and login
        var email = TestConstants.Users.GenerateUniqueEmail();
        var registerDto = TestConstants.DtoBuilders.CreateValidUserDto(
            email: email,
            password: TestConstants.Passwords.ValidPassword
        );
        await _client.PostAsJsonAsync(TestConstants.ApiPaths.AuthRegister, registerDto);

        var loginDto = TestConstants.DtoBuilders.CreateLoginDto(
            email: email,
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
        var revokeResponse = await _client.PostAsJsonAsync(
            TestConstants.ApiPaths.AuthRevoke,
            revokeDto
        );

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
        var refreshResponse = await _client.PostAsJsonAsync(
            TestConstants.ApiPaths.AuthRefresh,
            refreshDto
        );

        // Assert: Should fail
        refreshResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Revoke_WithoutToken_ShouldReturnBadRequest()
    {
        // Act
        var revokeDto = new RevokeRequestDto { RefreshToken = null };
        var response = await _client.PostAsJsonAsync(TestConstants.ApiPaths.AuthRevoke, revokeDto);

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
        var revokeResponse = await _client.PostAsJsonAsync(
            TestConstants.ApiPaths.AuthRevoke,
            revokeDto
        );

        // Assert
        revokeResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    /// <summary>
    /// Cenário Completo de Sucesso: Login → Refresh → Acesso a Endpoint Protegido
    /// </summary>
    [Fact]
    public async Task CompleteRefreshFlow_LoginRefreshAccessProtectedEndpoint_ShouldSucceed()
    {
        // Arrange: Register user
        var email = TestConstants.Users.GenerateUniqueEmail();
        var registerDto = TestConstants.DtoBuilders.CreateValidUserDto(
            email: email,
            password: TestConstants.Passwords.ValidPassword
        );
        await _client.PostAsJsonAsync(TestConstants.ApiPaths.AuthRegister, registerDto);

        // Act 1: Login to get initial tokens
        var loginDto = TestConstants.DtoBuilders.CreateLoginDto(
            email: email,
            password: TestConstants.Passwords.ValidPassword
        );
        var loginResponse = await _client.PostAsJsonAsync(
            TestConstants.ApiPaths.AuthLogin,
            loginDto
        );
        loginResponse.ShouldBeOk();

        var loginData = await loginResponse.Content.ReadFromJsonAsync<
            ApiResponse<LoginResponseDto>
        >();
        var originalAccessToken = loginData!.Data!.Token;
        var userId = loginData.Data.User!.Id;

        // Extract refresh token from cookie
        var cookies = loginResponse.Headers.GetValues("Set-Cookie").ToList();
        var refreshToken = cookies.First(c => c.Contains("refreshToken=")).Split(';')[0].Split('=')[
            1
        ];

        // Assert 1: Can access protected endpoint with original token
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", originalAccessToken);
        var protectedResponse1 = await _client.GetAsync(TestConstants.ApiPaths.UsersMe);
        protectedResponse1.ShouldBeOk();

        // Act 2: Use refresh token to get new access token
        var refreshDto = new RefreshRequestDto { RefreshToken = refreshToken };
        var refreshResponse = await _client.PostAsJsonAsync(
            TestConstants.ApiPaths.AuthRefresh,
            refreshDto
        );
        refreshResponse.ShouldBeOk();

        var refreshData = await refreshResponse.Content.ReadFromJsonAsync<
            ApiResponse<LoginResponseDto>
        >();
        var newAccessToken = refreshData!.Data!.Token;
        newAccessToken.Should().NotBeNullOrEmpty();
        newAccessToken.Should().NotBe(originalAccessToken, "should generate a new access token");

        // Assert 2: Can access protected endpoint with new token
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", newAccessToken);
        var protectedResponse2 = await _client.GetAsync(TestConstants.ApiPaths.UsersMe);
        protectedResponse2.ShouldBeOk();

        // Assert 3: New refresh token cookie is set
        var newCookies = refreshResponse.Headers.GetValues("Set-Cookie");
        newCookies.Should().Contain(c => c.Contains("refreshToken="));

        // Clean up
        _client.DefaultRequestHeaders.Authorization = null;
    }

    /// <summary>
    /// Cenário de Segurança: Reutilização de Token Detectada - Deve Revogar Todas as Sessões
    /// </summary>
    [Fact]
    public async Task TokenReuse_WhenDetected_ShouldRevokeAllUserTokens()
    {
        // Arrange: Register and login
        var email = TestConstants.Users.GenerateUniqueEmail();
        var registerDto = TestConstants.DtoBuilders.CreateValidUserDto(
            email: email,
            password: TestConstants.Passwords.ValidPassword
        );
        await _client.PostAsJsonAsync(TestConstants.ApiPaths.AuthRegister, registerDto);

        var loginDto = TestConstants.DtoBuilders.CreateLoginDto(
            email: email,
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

        // Act 1: Rotate token (this marks the old token as replaced)
        var firstRefreshDto = new RefreshRequestDto { RefreshToken = oldRefreshToken };
        var firstRefreshResponse = await _client.PostAsJsonAsync(
            TestConstants.ApiPaths.AuthRefresh,
            firstRefreshDto
        );
        firstRefreshResponse.ShouldBeOk();

        var firstRefreshData = await firstRefreshResponse.Content.ReadFromJsonAsync<
            ApiResponse<LoginResponseDto>
        >();
        firstRefreshData.Should().NotBeNull();

        // Extract the new refresh token
        var newCookies = firstRefreshResponse.Headers.GetValues("Set-Cookie").ToList();
        var newRefreshToken = newCookies
            .First(c => c.Contains("refreshToken="))
            .Split(';')[0]
            .Split('=')[1];
        newRefreshToken.Should().NotBe(oldRefreshToken);

        // Act 2: Attempt to reuse the old (replaced) token - SECURITY BREACH ATTEMPT
        var reuseRefreshDto = new RefreshRequestDto { RefreshToken = oldRefreshToken };
        var reuseRefreshResponse = await _client.PostAsJsonAsync(
            TestConstants.ApiPaths.AuthRefresh,
            reuseRefreshDto
        );

        // Assert 1: Request should fail
        reuseRefreshResponse.ShouldBeBadRequest();

        // Assert 2: Verify that the old token is marked as revoked and has attempted reuse reason
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var user = await db.Users.FirstAsync(u => u.Email == email);
        var oldTokenHash = RefreshTokenHelper.ComputeSha256Hash(oldRefreshToken);
        var oldTokenInDb = await db.RefreshTokens.FirstAsync(t => t.TokenHash == oldTokenHash);

        // The old token that was reused should be revoked
        oldTokenInDb.RevokedAt.Should().NotBeNull();
        oldTokenInDb.ReasonRevoked.Should().Contain("Rotated");

        // Assert 3: Verify that attempting to use the old token again still fails
        var thirdAttemptDto = new RefreshRequestDto { RefreshToken = oldRefreshToken };
        var thirdAttemptResponse = await _client.PostAsJsonAsync(
            TestConstants.ApiPaths.AuthRefresh,
            thirdAttemptDto
        );
        thirdAttemptResponse.ShouldBeBadRequest();
    }

    /// <summary>
    /// Cenário de Revogação Explícita: Logout via /auth/revoke
    /// </summary>
    [Fact]
    public async Task Revoke_ExplicitLogout_ShouldInvalidateTokenAndPreventFurtherUse()
    {
        // Arrange: Register and login
        var email = TestConstants.Users.GenerateUniqueEmail();
        var registerDto = TestConstants.DtoBuilders.CreateValidUserDto(
            email: email,
            password: TestConstants.Passwords.ValidPassword
        );
        await _client.PostAsJsonAsync(TestConstants.ApiPaths.AuthRegister, registerDto);

        var loginDto = TestConstants.DtoBuilders.CreateLoginDto(
            email: email,
            password: TestConstants.Passwords.ValidPassword
        );
        var loginResponse = await _client.PostAsJsonAsync(
            TestConstants.ApiPaths.AuthLogin,
            loginDto
        );
        loginResponse.ShouldBeOk();

        var loginData = await loginResponse.Content.ReadFromJsonAsync<
            ApiResponse<LoginResponseDto>
        >();
        var accessToken = loginData!.Data!.Token;
        var userId = loginData.Data.User!.Id;

        // Extract refresh token
        var cookies = loginResponse.Headers.GetValues("Set-Cookie").ToList();
        var refreshToken = cookies.First(c => c.Contains("refreshToken=")).Split(';')[0].Split('=')[
            1
        ];

        // Verify token works before revocation
        var refreshBeforeDto = new RefreshRequestDto { RefreshToken = refreshToken };
        var refreshBeforeResponse = await _client.PostAsJsonAsync(
            TestConstants.ApiPaths.AuthRefresh,
            refreshBeforeDto
        );
        refreshBeforeResponse.ShouldBeOk();

        // Extract the new token from that refresh
        var newCookies = refreshBeforeResponse.Headers.GetValues("Set-Cookie").ToList();
        var newRefreshToken = newCookies
            .First(c => c.Contains("refreshToken="))
            .Split(';')[0]
            .Split('=')[1];

        // Act: Explicitly revoke the new token (simulating logout)
        var revokeDto = new RevokeRequestDto { RefreshToken = newRefreshToken };
        var revokeResponse = await _client.PostAsJsonAsync(
            TestConstants.ApiPaths.AuthRevoke,
            revokeDto
        );

        // Assert 1: Revocation succeeds
        revokeResponse.ShouldBeOk();
        var revokeData = await revokeResponse.Content.ReadFromJsonAsync<ApiResponse<string>>();
        revokeData!.Success.Should().BeTrue();
        revokeData.Data.Should().Contain("revoked");

        // Assert 2: Cookie should be deleted (expires in the past)
        var revokeCookies = revokeResponse.Headers.GetValues("Set-Cookie");
        revokeCookies
            .Should()
            .Contain(c =>
                c.Contains("refreshToken=") && (c.Contains("expires=") || c.Contains("max-age=0"))
            );

        // Assert 3: Verify token is revoked in database
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var tokenHash = RefreshTokenHelper.ComputeSha256Hash(newRefreshToken);
        var token = await db.RefreshTokens.FirstAsync(t => t.TokenHash == tokenHash);
        token.RevokedAt.Should().NotBeNull();
        token.ReasonRevoked.Should().Contain("Revoked by user");

        // Assert 4: Cannot use revoked token
        var refreshAfterDto = new RefreshRequestDto { RefreshToken = newRefreshToken };
        var refreshAfterResponse = await _client.PostAsJsonAsync(
            TestConstants.ApiPaths.AuthRefresh,
            refreshAfterDto
        );
        refreshAfterResponse.ShouldBeBadRequest();

        // Assert 5: Access token still works (until it expires naturally)
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
        var protectedResponse = await _client.GetAsync(TestConstants.ApiPaths.UsersMe);
        protectedResponse.ShouldBeOk();
        // Note: access token should still work until it expires naturally

        // Clean up
        _client.DefaultRequestHeaders.Authorization = null;
    }
}
