using FluentAssertions;
using PetHub.API.Data;
using PetHub.API.DTOs.Common;
using PetHub.API.DTOs.User;
using PetHub.API.Services;
using PetHub.API.Utils;
using PetHub.Tests.Extensions;
using PetHub.Tests.IntegrationTests.Infrastructure;

namespace PetHub.Tests.IntegrationTests.Controllers.AuthControllerTests;

public class AuthLifecycleTests : IClassFixture<PetHubWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly PetHubWebApplicationFactory _factory;

    public AuthLifecycleTests(PetHubWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Register_SendsVerificationEmail_AndVerifyEmailSucceeds()
    {
        var registerDto = TestConstants.DtoBuilders.CreateValidUserDto();
        var registerResponse = await _client.PostAsJsonAsync(
            TestConstants.ApiPaths.AuthRegister,
            registerDto
        );

        var registered = await registerResponse
            .ShouldBeOk()
            .WithContent<ApiResponse<LoginResponseDto>>();
        registered.Data!.User.EmailVerified.Should().BeFalse();

        var token = _factory.EmailSender.RequireTokenFor(registerDto.Email);

        var verifyResponse = await _client.PostAsJsonAsync(
            TestConstants.ApiPaths.AuthVerifyEmail,
            new VerifyEmailDto { Token = token }
        );

        var verified = await verifyResponse.ShouldBeOk().WithContent<ApiResponse<string>>();
        verified.Success.Should().BeTrue();
        verified.Data.Should().Be(AuthLifecycleService.EmailVerifiedMessage);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var user = db.Users.Single(u => u.Email == registerDto.Email);
        user.EmailVerified.Should().BeTrue();
        user.EmailVerifiedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task VerifyEmail_WithInvalidToken_ReturnsBadRequest()
    {
        var response = await _client.PostAsJsonAsync(
            TestConstants.ApiPaths.AuthVerifyEmail,
            new VerifyEmailDto { Token = "not-a-real-token" }
        );

        await response
            .ShouldBeBadRequest()
            .WithErrorMessage(AuthLifecycleService.InvalidTokenMessage);
    }

    [Fact]
    public async Task ForgotPassword_UnknownEmail_ReturnsGenericSuccess()
    {
        var response = await _client.PostAsJsonAsync(
            TestConstants.ApiPaths.AuthForgotPassword,
            new ForgotPasswordDto { Email = "nobody@example.com" }
        );

        var apiResponse = await response.ShouldBeOk().WithContent<ApiResponse<string>>();
        apiResponse.Data.Should().Be(AuthLifecycleService.GenericPasswordResetMessage);
        _factory.EmailSender.FindByRecipient("nobody@example.com").Should().BeNull();
    }

    [Fact]
    public async Task ForgotPassword_ThenReset_AllowsLoginWithNewPassword()
    {
        var registerDto = TestConstants.DtoBuilders.CreateValidUserDto();
        await _client.PostAsJsonAsync(TestConstants.ApiPaths.AuthRegister, registerDto);

        var forgotResponse = await _client.PostAsJsonAsync(
            TestConstants.ApiPaths.AuthForgotPassword,
            new ForgotPasswordDto { Email = registerDto.Email }
        );
        var forgot = await forgotResponse.ShouldBeOk().WithContent<ApiResponse<string>>();
        forgot.Data.Should().Be(AuthLifecycleService.GenericPasswordResetMessage);

        var token = _factory.EmailSender.RequireTokenFor(registerDto.Email);
        var newPassword = TestConstants.Passwords.AnotherValidPassword;

        var resetResponse = await _client.PostAsJsonAsync(
            TestConstants.ApiPaths.AuthResetPassword,
            new ResetPasswordDto { Token = token, NewPassword = newPassword }
        );
        var reset = await resetResponse.ShouldBeOk().WithContent<ApiResponse<string>>();
        reset.Data.Should().Be(AuthLifecycleService.PasswordResetSuccessMessage);

        var oldPasswordLogin = await _client.PostAsJsonAsync(
            TestConstants.ApiPaths.AuthLogin,
            TestConstants.DtoBuilders.CreateLoginDto(registerDto.Email, registerDto.Password)
        );
        oldPasswordLogin.ShouldBeUnauthorized();

        var newPasswordLogin = await _client.PostAsJsonAsync(
            TestConstants.ApiPaths.AuthLogin,
            TestConstants.DtoBuilders.CreateLoginDto(registerDto.Email, newPassword)
        );
        newPasswordLogin.ShouldBeOk();
    }

    [Fact]
    public async Task ResetPassword_WithInvalidToken_ReturnsBadRequest()
    {
        var response = await _client.PostAsJsonAsync(
            TestConstants.ApiPaths.AuthResetPassword,
            new ResetPasswordDto
            {
                Token = "invalid",
                NewPassword = TestConstants.Passwords.AnotherValidPassword,
            }
        );

        await response
            .ShouldBeBadRequest()
            .WithErrorMessage(AuthLifecycleService.InvalidTokenMessage);
    }

    [Fact]
    public async Task ResendVerification_SendsNewToken_AndInvalidatesPrevious()
    {
        var registerDto = TestConstants.DtoBuilders.CreateValidUserDto();
        await _client.PostAsJsonAsync(TestConstants.ApiPaths.AuthRegister, registerDto);
        var firstToken = _factory.EmailSender.RequireTokenFor(registerDto.Email);

        var resend = await _client.PostAsJsonAsync(
            TestConstants.ApiPaths.AuthResendVerification,
            new ResendVerificationDto { Email = registerDto.Email }
        );
        var apiResponse = await resend.ShouldBeOk().WithContent<ApiResponse<string>>();
        apiResponse.Data.Should().Be(AuthLifecycleService.GenericVerificationMessage);

        var secondToken = _factory.EmailSender.RequireTokenFor(registerDto.Email);
        secondToken.Should().NotBe(firstToken);

        var firstVerify = await _client.PostAsJsonAsync(
            TestConstants.ApiPaths.AuthVerifyEmail,
            new VerifyEmailDto { Token = firstToken }
        );
        firstVerify.ShouldBeBadRequest();

        var secondVerify = await _client.PostAsJsonAsync(
            TestConstants.ApiPaths.AuthVerifyEmail,
            new VerifyEmailDto { Token = secondToken }
        );
        secondVerify.ShouldBeOk();
    }

    [Fact]
    public async Task ResendVerification_UnknownEmail_ReturnsGenericSuccess()
    {
        var response = await _client.PostAsJsonAsync(
            TestConstants.ApiPaths.AuthResendVerification,
            new ResendVerificationDto { Email = "ghost@example.com" }
        );

        var apiResponse = await response.ShouldBeOk().WithContent<ApiResponse<string>>();
        apiResponse.Data.Should().Be(AuthLifecycleService.GenericVerificationMessage);
    }

    [Fact]
    public async Task ResetPassword_RevokesExistingRefreshToken()
    {
        var registerDto = TestConstants.DtoBuilders.CreateValidUserDto();
        var registerResponse = await _client.PostAsJsonAsync(
            TestConstants.ApiPaths.AuthRegister,
            registerDto
        );
        registerResponse.ShouldBeOk();

        var refreshCookie = registerResponse
            .Headers.GetValues("Set-Cookie")
            .First(c => c.Contains("refreshToken="));
        var refreshToken = ExtractCookieValue(refreshCookie);

        await _client.PostAsJsonAsync(
            TestConstants.ApiPaths.AuthForgotPassword,
            new ForgotPasswordDto { Email = registerDto.Email }
        );
        var token = _factory.EmailSender.RequireTokenFor(registerDto.Email);
        await _client.PostAsJsonAsync(
            TestConstants.ApiPaths.AuthResetPassword,
            new ResetPasswordDto
            {
                Token = token,
                NewPassword = TestConstants.Passwords.AnotherValidPassword,
            }
        );

        var refreshResponse = await _client.PostAsJsonAsync(
            TestConstants.ApiPaths.AuthRefresh,
            new RefreshRequestDto { RefreshToken = refreshToken }
        );
        refreshResponse.ShouldBeBadRequest();
    }

    private static string ExtractCookieValue(string setCookieHeader)
    {
        var start = setCookieHeader.IndexOf('=') + 1;
        var end = setCookieHeader.IndexOf(';');
        return setCookieHeader[start..end];
    }
}
