using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using PetHub.API.Configuration;
using PetHub.API.Data;
using PetHub.API.DTOs.User;
using PetHub.API.Enums;
using PetHub.API.Models;
using PetHub.API.Services;
using PetHub.API.Utils;

namespace PetHub.Tests.UnitTests.Services;

public class AuthLifecycleServiceTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly Mock<IEmailSender> _emailSender;
    private readonly Mock<IRefreshTokenService> _refreshTokenService;
    private readonly AuthLifecycleService _service;
    private readonly AuthLifecycleSettings _settings;

    public AuthLifecycleServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new AppDbContext(options);
        _emailSender = new Mock<IEmailSender>();
        _refreshTokenService = new Mock<IRefreshTokenService>();
        _settings = new AuthLifecycleSettings
        {
            EmailVerificationExpiresHours = 24,
            PasswordResetExpiresHours = 1,
            FrontendBaseUrl = "http://localhost:5173",
        };

        _service = new AuthLifecycleService(
            _context,
            _emailSender.Object,
            _refreshTokenService.Object,
            Options.Create(_settings),
            TimeProvider.System,
            NullLogger<AuthLifecycleService>.Instance
        );
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task SendVerificationEmailAsync_PersistsHashedTokenAndSendsEmail()
    {
        var user = await CreateUserAsync();
        string? capturedBody = null;
        _emailSender
            .Setup(s =>
                s.SendAsync(
                    user.Email,
                    AuthEmailTemplates.VerificationSubject,
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .Callback<string, string, string, string, CancellationToken>(
                (_, _, text, _, _) => capturedBody = text
            )
            .Returns(Task.CompletedTask);

        await _service.SendVerificationEmailAsync(user);

        capturedBody.Should().NotBeNull();
        var plainToken = AuthEmailTemplates.ExtractToken(capturedBody!);
        plainToken.Should().NotBeNullOrEmpty();

        var stored = await _context.AuthTokens.SingleAsync();
        stored.UserId.Should().Be(user.Id);
        stored.Purpose.Should().Be(AuthTokenPurpose.EmailVerification);
        stored.UsedAt.Should().BeNull();
        stored.TokenHash.Should().Be(RefreshTokenHelper.ComputeSha256Hash(plainToken!));
        stored.TokenHash.Should().NotBe(plainToken);
        capturedBody.Should().Contain("/verify-email?token=");
    }

    [Fact]
    public async Task SendVerificationEmailAsync_WhenAlreadyVerified_DoesNotSend()
    {
        var user = await CreateUserAsync(verified: true);

        await _service.SendVerificationEmailAsync(user);

        _emailSender.Verify(
            s =>
                s.SendAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()
                ),
            Times.Never
        );
        (await _context.AuthTokens.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task VerifyEmailAsync_MarksUserAsVerifiedAndConsumesToken()
    {
        var user = await CreateUserAsync();
        var token = await IssueVerificationTokenAsync(user);

        await _service.VerifyEmailAsync(token);

        var stored = await _context.Users.FindAsync(user.Id);
        stored!.EmailVerified.Should().BeTrue();
        stored.EmailVerifiedAt.Should().NotBeNull();
        (await _context.AuthTokens.SingleAsync()).UsedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task VerifyEmailAsync_ReusedToken_Throws()
    {
        var user = await CreateUserAsync();
        var token = await IssueVerificationTokenAsync(user);
        await _service.VerifyEmailAsync(token);

        var act = async () => await _service.VerifyEmailAsync(token);

        await act.Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage(AuthLifecycleService.InvalidTokenMessage);
    }

    [Fact]
    public async Task VerifyEmailAsync_ExpiredToken_Throws()
    {
        var user = await CreateUserAsync();
        var token = RefreshTokenHelper.GenerateSecureToken();
        _context.AuthTokens.Add(
            new AuthToken
            {
                UserId = user.Id,
                TokenHash = RefreshTokenHelper.ComputeSha256Hash(token),
                Purpose = AuthTokenPurpose.EmailVerification,
                CreatedAt = DateTime.UtcNow.AddHours(-25),
                ExpiresAt = DateTime.UtcNow.AddMinutes(-1),
            }
        );
        await _context.SaveChangesAsync();

        var act = async () => await _service.VerifyEmailAsync(token);

        await act.Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage(AuthLifecycleService.InvalidTokenMessage);
        (await _context.Users.FindAsync(user.Id))!.EmailVerified.Should().BeFalse();
    }

    [Fact]
    public async Task VerifyEmailAsync_PasswordResetToken_Throws()
    {
        var user = await CreateUserAsync();
        var token = await IssuePasswordResetTokenAsync(user);

        var act = async () => await _service.VerifyEmailAsync(token);

        await act.Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage(AuthLifecycleService.InvalidTokenMessage);
    }

    [Fact]
    public async Task RequestVerificationEmailAsync_UnknownEmail_DoesNotThrowOrSend()
    {
        await _service.RequestVerificationEmailAsync("missing@example.com");

        _emailSender.Verify(
            s =>
                s.SendAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()
                ),
            Times.Never
        );
    }

    [Fact]
    public async Task RequestPasswordResetAsync_UnknownEmail_DoesNotThrowOrSend()
    {
        await _service.RequestPasswordResetAsync("missing@example.com");

        _emailSender.Verify(
            s =>
                s.SendAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()
                ),
            Times.Never
        );
        (await _context.AuthTokens.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task ResetPasswordAsync_UpdatesPasswordAndRevokesSessions()
    {
        var user = await CreateUserAsync();
        var oldHash = user.PasswordHash;
        var token = await IssuePasswordResetTokenAsync(user);

        await _service.ResetPasswordAsync(token, TestConstants.Passwords.AnotherValidPassword);

        var stored = await _context.Users.FindAsync(user.Id);
        stored!.PasswordHash.Should().NotBe(oldHash);
        PasswordHelper
            .VerifyPassword(TestConstants.Passwords.AnotherValidPassword, stored.PasswordHash)
            .Should()
            .BeTrue();
        _refreshTokenService.Verify(
            s => s.RevokeAllForUserAsync(user.Id, "Password reset", It.IsAny<CancellationToken>()),
            Times.Once
        );
    }

    [Fact]
    public async Task SendVerificationEmailAsync_InvalidatesPreviousUnusedTokens()
    {
        var user = await CreateUserAsync();
        var first = await IssueVerificationTokenAsync(user);

        await _service.SendVerificationEmailAsync(user);

        var tokens = await _context.AuthTokens.ToListAsync();
        tokens.Should().HaveCount(2);
        tokens.Count(t => t.UsedAt == null).Should().Be(1);

        var act = async () => await _service.VerifyEmailAsync(first);
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task SendVerificationEmailAsync_WhenEmailFails_DoesNotThrow()
    {
        var user = await CreateUserAsync();
        _emailSender
            .Setup(s =>
                s.SendAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ThrowsAsync(new InvalidOperationException("SMTP down"));

        var act = async () => await _service.SendVerificationEmailAsync(user);

        await act.Should().NotThrowAsync();
        (await _context.AuthTokens.CountAsync()).Should().Be(1);
    }

    private async Task<string> IssueVerificationTokenAsync(User user)
    {
        string? body = null;
        _emailSender
            .Setup(s =>
                s.SendAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .Callback<string, string, string, string, CancellationToken>(
                (_, _, text, _, _) => body = text
            )
            .Returns(Task.CompletedTask);

        await _service.SendVerificationEmailAsync(user);
        return AuthEmailTemplates.ExtractToken(body!)!;
    }

    private async Task<string> IssuePasswordResetTokenAsync(User user)
    {
        string? body = null;
        _emailSender
            .Setup(s =>
                s.SendAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .Callback<string, string, string, string, CancellationToken>(
                (_, _, text, _, _) => body = text
            )
            .Returns(Task.CompletedTask);

        await _service.RequestPasswordResetAsync(user.Email);
        return AuthEmailTemplates.ExtractToken(body!)!;
    }

    private async Task<User> CreateUserAsync(bool verified = false)
    {
        var user = await new UserRepository(_context).CreateAsync(
            TestConstants.DtoBuilders.CreateValidUserDto()
        );
        if (verified)
        {
            user.EmailVerified = true;
            user.EmailVerifiedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        return user;
    }
}
