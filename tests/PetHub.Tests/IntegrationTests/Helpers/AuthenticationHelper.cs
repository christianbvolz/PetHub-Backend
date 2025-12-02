using System.Net.Http.Json;
using PetHub.API.DTOs.Common;
using PetHub.API.DTOs.User;

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
        email ??= $"test-{Guid.NewGuid()}@example.com";

        var registerDto = new CreateUserDto
        {
            Name = "Test User",
            Email = email,
            Password = "test123456",
            PhoneNumber = "11999887766",
            ZipCode = "01310100",
            State = "SP",
            City = "São Paulo",
            Neighborhood = "Centro",
            Street = "Rua Test",
            StreetNumber = "123",
        };

        var response = await client.PostAsJsonAsync("/api/auth/register", registerDto);
        response.EnsureSuccessStatusCode();

        var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<LoginResponseDto>>();
        return apiResponse?.Data?.Token
            ?? throw new InvalidOperationException("Token not received from registration");
    }

    public static async Task<string> LoginAndGetTokenAsync(
        HttpClient client,
        string email = "test@example.com",
        string password = "test123456"
    )
    {
        var loginDto = new LoginDto { Email = email, Password = password };

        var response = await client.PostAsJsonAsync("/api/auth/login", loginDto);
        response.EnsureSuccessStatusCode();

        var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<LoginResponseDto>>();
        return apiResponse?.Data?.Token
            ?? throw new InvalidOperationException("Token not received from login");
    }

    public static void AddAuthToken(this HttpClient client, string token)
    {
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
    }
}
