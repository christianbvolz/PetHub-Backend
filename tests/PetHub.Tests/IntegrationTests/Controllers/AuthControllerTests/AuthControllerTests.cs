using FluentAssertions;
using PetHub.API.DTOs.Common;
using PetHub.API.DTOs.User;
using PetHub.Tests.Extensions;
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
        var registerDto = TestConstants.DtoBuilders.CreateValidUserDto();

        var response = await _client.PostAsJsonAsync(
            TestConstants.ApiPaths.AuthRegister,
            registerDto
        );

        var apiResponse = await response.ShouldBeOk().WithContent<ApiResponse<LoginResponseDto>>();
        apiResponse.Success.Should().BeTrue();
        apiResponse.Data.Should().NotBeNull();
        apiResponse.Data!.Token.Should().NotBeNullOrEmpty();
        apiResponse.Data.User.Should().NotBeNull();
        apiResponse.Data.User.Email.Should().Be(registerDto.Email);
        apiResponse.Data.User.Name.Should().Be(registerDto.Name);
    }

    [Fact]
    public async Task Register_WithDuplicateEmail_ReturnsBadRequest()
    {
        var registerDto = TestConstants.DtoBuilders.CreateValidUserDto();

        await _client.PostAsJsonAsync(TestConstants.ApiPaths.AuthRegister, registerDto);

        var response = await _client.PostAsJsonAsync(
            TestConstants.ApiPaths.AuthRegister,
            registerDto
        );

        await response.ShouldBeBadRequest().WithErrorMessage("already registered");
    }

    [Fact]
    public async Task Register_WithInvalidEmail_ReturnsBadRequest()
    {
        var registerDto = TestConstants.DtoBuilders.CreateValidUserDto(
            email: TestConstants.Users.InvalidEmail
        );

        var response = await _client.PostAsJsonAsync(
            TestConstants.ApiPaths.AuthRegister,
            registerDto
        );

        response.ShouldBeBadRequest();
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsToken()
    {
        var registerDto = TestConstants.DtoBuilders.CreateValidUserDto(
            email: TestConstants.Users.AnotherEmail,
            password: TestConstants.Passwords.ValidPassword
        );
        await _client.PostAsJsonAsync(TestConstants.ApiPaths.AuthRegister, registerDto);

        var loginDto = TestConstants.DtoBuilders.CreateLoginDto(
            email: TestConstants.Users.AnotherEmail,
            password: TestConstants.Passwords.ValidPassword
        );
        var response = await _client.PostAsJsonAsync(TestConstants.ApiPaths.AuthLogin, loginDto);

        response.ShouldBeOk();

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
        var loginDto = TestConstants.DtoBuilders.CreateLoginDto(
            email: TestConstants.Users.AnotherEmail,
            password: TestConstants.Passwords.ValidPassword
        );

        var response = await _client.PostAsJsonAsync(TestConstants.ApiPaths.AuthLogin, loginDto);

        response.ShouldBeUnauthorized();
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Invalid email or password");
    }

    [Fact]
    public async Task Login_WithInvalidPassword_ReturnsUnauthorized()
    {
        var registerDto = TestConstants.DtoBuilders.CreateValidUserDto();

        await _client.PostAsJsonAsync(TestConstants.ApiPaths.AuthRegister, registerDto);

        var loginDto = TestConstants.DtoBuilders.CreateLoginDto(
            email: TestConstants.Users.AnotherEmail,
            password: TestConstants.Passwords.AnotherValidPassword
        );

        var response = await _client.PostAsJsonAsync(TestConstants.ApiPaths.AuthLogin, loginDto);

        response.ShouldBeUnauthorized();
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Invalid email or password");
    }

    [Fact]
    public async Task Login_TokenCanBeUsedForAuthentication()
    {
        var registerDto = TestConstants.DtoBuilders.CreateValidUserDto();

        await _client.PostAsJsonAsync(TestConstants.ApiPaths.AuthRegister, registerDto);

        var loginDto = TestConstants.DtoBuilders.CreateLoginDto(
            email: registerDto.Email,
            password: registerDto.Password
        );

        var loginResponse = await _client.PostAsJsonAsync(
            TestConstants.ApiPaths.AuthLogin,
            loginDto
        );

        var apiResponse = await loginResponse.Content.ReadFromJsonAsync<
            ApiResponse<LoginResponseDto>
        >();
        var token = apiResponse!.Data!.Token;

        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        var protectedResponse = await _client.GetAsync(TestConstants.ApiPaths.UsersMe);

        protectedResponse.ShouldBeOk();
    }
}
