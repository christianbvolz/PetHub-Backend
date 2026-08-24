using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PetHub.API.Configuration;
using PetHub.API.Data;
using PetHub.API.Models;
using PetHub.API.Services;

namespace PetHub.Tests.UnitTests.Services;

public class RefreshTokenServiceTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly RefreshTokenService _service;

    public RefreshTokenServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new AppDbContext(options);
        _service = new RefreshTokenService(
            _context,
            Options.Create(new RefreshTokenSettings { ExpiresAtDays = 14 })
        );
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task RevokeAllForUserAsync_RevokesOnlyActiveTokensForThatUser()
    {
        var user = await CreateUserAsync("owner@example.com");
        var other = await CreateUserAsync("other@example.com");
        var token = await _service.CreateAsync(user.Id);
        var otherToken = await _service.CreateAsync(other.Id);

        await _service.RevokeAllForUserAsync(user.Id, "Password reset");

        var userToken = await _service.GetByTokenAsync(token);
        var remaining = await _service.GetByTokenAsync(otherToken);

        userToken.Should().NotBeNull();
        userToken!.RevokedAt.Should().NotBeNull();
        userToken.ReasonRevoked.Should().Be("Password reset");
        remaining.Should().NotBeNull();
        remaining!.RevokedAt.Should().BeNull();
    }

    private async Task<User> CreateUserAsync(string email)
    {
        var user = await new UserRepository(_context).CreateAsync(
            TestConstants.DtoBuilders.CreateValidUserDto(email: email)
        );
        return user;
    }
}
