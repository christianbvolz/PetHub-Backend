using System.Net;
using PetHub.API.DTOs.Common;
using PetHub.API.DTOs.User;
using PetHub.Tests.Extensions;

namespace PetHub.Tests.IntegrationTests.Helpers;

/// <summary>
/// Provides helper methods for registering users, logging in, and managing authentication tokens
/// in integration test scenarios. Intended for use across multiple test files to facilitate
/// authentication setup and token management in test environments.
/// </summary>
public static class AuthenticationHelper
{
    public static async Task<string> RegisterAndGetTokenAsync(
        HttpClient client,
        string? email = null
    )
    {
        // Generate unique email if not provided to avoid conflicts in tests
        email ??= TestConstants.Users.GenerateUniqueEmail();

        var registerDto = TestConstants.DtoBuilders.CreateValidUserDto(
            email: email,
            name: TestConstants.Users.Username
        );

        var response = await client.PostAsJsonAsync(
            TestConstants.ApiPaths.AuthRegister,
            registerDto
        );

        // If registration succeeded return token
        if (response.IsSuccessStatusCode)
        {
            var apiResponse = await response
                .ShouldBeOk()
                .WithContent<ApiResponse<LoginResponseDto>>();
            return apiResponse.Data?.Token
                ?? throw new InvalidOperationException("Token not received from registration");
        }

        // If user already exists (bad request due to duplicate email), try to login
        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            return await LoginAndGetTokenAsync(
                client,
                email ?? registerDto.Email,
                TestConstants.Passwords.ValidPassword
            );
        }

        // Unexpected response
        throw new InvalidOperationException(
            $"Registration failed with status code {response.StatusCode}"
        );
    }

    public static async Task<string> LoginAndGetTokenAsync(
        HttpClient client,
        string email = TestConstants.Users.Email,
        string password = TestConstants.Passwords.DefaultAuthPassword
    )
    {
        var loginDto = TestConstants.DtoBuilders.CreateLoginDto(email: email, password: password);

        var response = await client.PostAsJsonAsync(TestConstants.ApiPaths.AuthLogin, loginDto);

        var apiResponse = await response.ShouldBeOk().WithContent<ApiResponse<LoginResponseDto>>();
        return apiResponse.Data?.Token
            ?? throw new InvalidOperationException("Token not received from login");
    }

    public static void AddAuthToken(this HttpClient client, string token)
    {
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
    }
}
