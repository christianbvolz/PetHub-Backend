using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using PetHub.API.DTOs.Common;
using PetHub.API.DTOs.User;
using PetHub.Tests.IntegrationTests.Infrastructure;

namespace PetHub.Tests.IntegrationTests.Controllers.AuthControllerTests;

public class AuthControllerTests : IClassFixture<PetHubWebApplicationFactory>
{
    private readonly HttpClient _client;

    public AuthControllerTests(PetHubWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Register_WithValidData_ReturnsCreatedAndToken()
    {
        var registerDto = new CreateUserDto
        {
            Name = "Test User",
            Email = "test@example.com",
            Password = "password123",
            PhoneNumber = "11999887766",
            ZipCode = "01310100",
            State = "SP",
            City = "São Paulo",
            Neighborhood = "Centro",
            Street = "Rua Teste",
            StreetNumber = "123",
        };

        var response = await _client.PostAsJsonAsync("/api/auth/register", registerDto);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<LoginResponseDto>>();
        apiResponse.Should().NotBeNull();
        apiResponse!.Success.Should().BeTrue();
        apiResponse.Data.Should().NotBeNull();
        apiResponse.Data!.Token.Should().NotBeNullOrEmpty();
        apiResponse.Data.User.Should().NotBeNull();
        apiResponse.Data.User.Email.Should().Be(registerDto.Email);
        apiResponse.Data.User.Name.Should().Be(registerDto.Name);
    }

    [Fact]
    public async Task Register_WithDuplicateEmail_ReturnsBadRequest()
    {
        var registerDto = new CreateUserDto
        {
            Name = "First User",
            Email = "duplicate@example.com",
            Password = "password123",
            PhoneNumber = "11999887766",
            ZipCode = "01310100",
            State = "SP",
            City = "São Paulo",
            Neighborhood = "Centro",
            Street = "Rua Teste",
            StreetNumber = "123",
        };

        await _client.PostAsJsonAsync("/api/auth/register", registerDto);
        var response = await _client.PostAsJsonAsync("/api/auth/register", registerDto);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("already registered");
    }

    [Fact]
    public async Task Register_WithInvalidEmail_ReturnsBadRequest()
    {
        var registerDto = new CreateUserDto
        {
            Name = "Test User",
            Email = "invalid-email",
            Password = "password123",
            PhoneNumber = "11999887766",
            ZipCode = "01310100",
            State = "SP",
            City = "São Paulo",
            Neighborhood = "Centro",
            Street = "Rua Teste",
            StreetNumber = "123",
        };

        var response = await _client.PostAsJsonAsync("/api/auth/register", registerDto);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsToken()
    {
        var registerDto = new CreateUserDto
        {
            Name = "Login Test User",
            Email = "login@example.com",
            Password = "password123",
            PhoneNumber = "11999887766",
            ZipCode = "01310100",
            State = "SP",
            City = "São Paulo",
            Neighborhood = "Centro",
            Street = "Rua Teste",
            StreetNumber = "123",
        };
        await _client.PostAsJsonAsync("/api/auth/register", registerDto);

        var loginDto = new LoginDto { Email = "login@example.com", Password = "password123" };
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginDto);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<LoginResponseDto>>();
        apiResponse.Should().NotBeNull();
        apiResponse!.Success.Should().BeTrue();
        apiResponse.Data.Should().NotBeNull();
        apiResponse.Data!.Token.Should().NotBeNullOrEmpty();
        apiResponse.Data.User.Email.Should().Be(loginDto.Email);
    }

    [Fact]
    public async Task Login_WithInvalidEmail_ReturnsUnauthorized()
    {
        var loginDto = new LoginDto { Email = "nonexistent@example.com", Password = "password123" };

        var response = await _client.PostAsJsonAsync("/api/auth/login", loginDto);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Invalid email or password");
    }

    [Fact]
    public async Task Login_WithInvalidPassword_ReturnsUnauthorized()
    {
        var registerDto = new CreateUserDto
        {
            Name = "Password Test User",
            Email = "passwordtest@example.com",
            Password = "correctpassword",
            PhoneNumber = "11999887766",
            ZipCode = "01310100",
            State = "SP",
            City = "São Paulo",
            Neighborhood = "Centro",
            Street = "Rua Teste",
            StreetNumber = "123",
        };
        await _client.PostAsJsonAsync("/api/auth/register", registerDto);

        var loginDto = new LoginDto
        {
            Email = "passwordtest@example.com",
            Password = "wrongpassword",
        };
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginDto);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Invalid email or password");
    }

    [Fact]
    public async Task Login_TokenCanBeUsedForAuthentication()
    {
        var registerDto = new CreateUserDto
        {
            Name = "Token Test User",
            Email = "tokentest@example.com",
            Password = "password123",
            PhoneNumber = "11999887766",
            ZipCode = "01310100",
            State = "SP",
            City = "São Paulo",
            Neighborhood = "Centro",
            Street = "Rua Teste",
            StreetNumber = "123",
        };
        await _client.PostAsJsonAsync("/api/auth/register", registerDto);

        var loginDto = new LoginDto { Email = "tokentest@example.com", Password = "password123" };
        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", loginDto);
        var apiResponse = await loginResponse.Content.ReadFromJsonAsync<
            ApiResponse<LoginResponseDto>
        >();
        var token = apiResponse!.Data!.Token;

        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        var protectedResponse = await _client.GetAsync("/api/users/me");

        protectedResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
