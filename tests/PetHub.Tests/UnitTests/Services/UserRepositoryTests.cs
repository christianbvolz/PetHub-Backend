using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using PetHub.API.Data;
using PetHub.API.DTOs.User;
using PetHub.API.Enums;
using PetHub.API.Services;

namespace PetHub.Tests.UnitTests.Services;

public class UserRepositoryTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly UserRepository _repository;

    public UserRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new AppDbContext(options);
        _repository = new UserRepository(_context);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task CreateAsync_Person_DefaultsAccountTypeAndOmitsCnpj()
    {
        var dto = TestConstants.DtoBuilders.CreateValidUserDto();

        var user = await _repository.CreateAsync(dto);

        user.AccountType.Should().Be(UserType.Person);
        user.Cnpj.Should().BeEmpty();
        user.Description.Should().BeEmpty();
    }

    [Fact]
    public async Task CreateAsync_Shelter_StoresNormalizedCnpjAndDescription()
    {
        var dto = TestConstants.DtoBuilders.CreateValidShelterDto(
            cnpj: TestConstants.Users.ValidFormattedCnpj
        );

        var user = await _repository.CreateAsync(dto);

        user.AccountType.Should().Be(UserType.Shelter);
        user.Cnpj.Should().Be(TestConstants.Users.ValidCnpj);
        user.Description.Should().Be(TestConstants.Users.ShelterDescription);
        user.Name.Should().Be(TestConstants.Users.ShelterName);
    }

    [Fact]
    public async Task CreateAsync_DuplicateCnpj_Throws()
    {
        await _repository.CreateAsync(TestConstants.DtoBuilders.CreateValidShelterDto());

        var duplicate = TestConstants.DtoBuilders.CreateValidShelterDto();

        var act = async () => await _repository.CreateAsync(duplicate);

        await act.Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage("CNPJ already registered.");
    }

    [Fact]
    public async Task UpdateAsync_PersonToShelterWithoutCnpj_Throws()
    {
        var user = await _repository.CreateAsync(TestConstants.DtoBuilders.CreateValidUserDto());

        var act = async () =>
            await _repository.UpdateAsync(
                user.Id,
                new PatchUserDto { AccountType = UserType.Shelter }
            );

        await act.Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage("A valid CNPJ is required for shelter accounts.");
    }

    [Fact]
    public async Task UpdateAsync_PersonToShelterWithCnpj_Succeeds()
    {
        var user = await _repository.CreateAsync(TestConstants.DtoBuilders.CreateValidUserDto());

        var updated = await _repository.UpdateAsync(
            user.Id,
            new PatchUserDto
            {
                AccountType = UserType.Shelter,
                Cnpj = TestConstants.Users.ValidCnpj,
                Description = TestConstants.Users.ShelterDescription,
            }
        );

        updated.Should().BeTrue();
        var stored = await _repository.GetByIdAsync(user.Id);
        stored!.AccountType.Should().Be(UserType.Shelter);
        stored.Cnpj.Should().Be(TestConstants.Users.ValidCnpj);
        stored.Description.Should().Be(TestConstants.Users.ShelterDescription);
    }

    [Fact]
    public async Task UpdateAsync_ShelterToPerson_ClearsCnpj()
    {
        var user = await _repository.CreateAsync(TestConstants.DtoBuilders.CreateValidShelterDto());

        await _repository.UpdateAsync(user.Id, new PatchUserDto { AccountType = UserType.Person });

        var stored = await _repository.GetByIdAsync(user.Id);
        stored!.AccountType.Should().Be(UserType.Person);
        stored.Cnpj.Should().BeEmpty();
    }

    [Fact]
    public async Task UpdateAsync_EmailChange_ResetsEmailVerification()
    {
        var user = await _repository.CreateAsync(TestConstants.DtoBuilders.CreateValidUserDto());
        user.EmailVerified = true;
        user.EmailVerifiedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        await _repository.UpdateAsync(
            user.Id,
            new PatchUserDto { Email = TestConstants.Users.AnotherEmail }
        );

        var stored = await _repository.GetByIdAsync(user.Id);
        stored!.Email.Should().Be(TestConstants.Users.AnotherEmail);
        stored.EmailVerified.Should().BeFalse();
        stored.EmailVerifiedAt.Should().BeNull();
    }

    [Fact]
    public async Task UpdateAsync_PasswordWithoutCurrentPassword_Throws()
    {
        var user = await _repository.CreateAsync(TestConstants.DtoBuilders.CreateValidUserDto());

        var act = async () =>
            await _repository.UpdateAsync(
                user.Id,
                new PatchUserDto { Password = TestConstants.Passwords.AnotherValidPassword }
            );

        await act.Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage("Current password is incorrect.");
    }

    [Fact]
    public async Task UpdateAsync_PasswordWithWrongCurrentPassword_Throws()
    {
        var user = await _repository.CreateAsync(TestConstants.DtoBuilders.CreateValidUserDto());

        var act = async () =>
            await _repository.UpdateAsync(
                user.Id,
                new PatchUserDto
                {
                    Password = TestConstants.Passwords.AnotherValidPassword,
                    CurrentPassword = "WrongPass1",
                }
            );

        await act.Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage("Current password is incorrect.");
    }

    [Fact]
    public async Task UpdateAsync_PasswordSameAsCurrent_Throws()
    {
        var user = await _repository.CreateAsync(TestConstants.DtoBuilders.CreateValidUserDto());

        var act = async () =>
            await _repository.UpdateAsync(
                user.Id,
                new PatchUserDto
                {
                    Password = TestConstants.Passwords.ValidPassword,
                    CurrentPassword = TestConstants.Passwords.ValidPassword,
                }
            );

        await act.Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage("New password must be different from the current password.");
    }

    [Fact]
    public async Task UpdateAsync_PasswordWithCurrentPassword_Succeeds()
    {
        var user = await _repository.CreateAsync(TestConstants.DtoBuilders.CreateValidUserDto());

        var updated = await _repository.UpdateAsync(
            user.Id,
            new PatchUserDto
            {
                Password = TestConstants.Passwords.AnotherValidPassword,
                CurrentPassword = TestConstants.Passwords.ValidPassword,
            }
        );

        updated.Should().BeTrue();
        var stored = await _repository.GetByIdAsync(user.Id);
        PetHub.API.Utils.PasswordHelper.VerifyPassword(
                TestConstants.Passwords.AnotherValidPassword,
                stored!.PasswordHash
            )
            .Should()
            .BeTrue();
    }
}
