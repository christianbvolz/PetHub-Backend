using System.Net.Http.Json;
using PetHub.API.DTOs.Common;
using PetHub.API.DTOs.User;
using PetHub.Tests;
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
        email ??= TestConstants.IntegrationTests.Emails.GenerateUnique();

        var registerDto = TestConstants.DtoBuilders.CreateValidUserDto(
            email: email,
            name: TestConstants.IntegrationTests.UserData.DefaultName
        );

        var response = await client.PostAsJsonAsync(
            TestConstants.IntegrationTests.ApiPaths.AuthRegister,
            registerDto
        );

        var apiResponse = await response.ShouldBeOk().WithContent<ApiResponse<LoginResponseDto>>();
        return apiResponse.Data?.Token
            ?? throw new InvalidOperationException("Token not received from registration");
    }

    public static async Task<string> LoginAndGetTokenAsync(
        HttpClient client,
        string email = TestConstants.IntegrationTests.Emails.DefaultAuthEmail,
        string password = TestConstants.IntegrationTests.Emails.DefaultAuthPassword
    )
    {
        var loginDto = TestConstants.DtoBuilders.CreateLoginDto(email: email, password: password);

        var response = await client.PostAsJsonAsync(
            TestConstants.IntegrationTests.ApiPaths.AuthLogin,
            loginDto
        );

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
