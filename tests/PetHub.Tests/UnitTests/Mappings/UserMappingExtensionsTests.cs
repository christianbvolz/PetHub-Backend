using System.Text.Json;
using FluentAssertions;
using PetHub.API.Enums;
using PetHub.API.Mappings;
using PetHub.API.Models;

namespace PetHub.Tests.UnitTests.Mappings;

public class UserMappingExtensionsTests
{
    [Fact]
    public void ToPublicResponseDto_OmitsPrivateContactAndAddressFields()
    {
        var user = CreateUser();

        var dto = user.ToPublicResponseDto();

        dto.Id.Should().Be(user.Id);
        dto.Name.Should().Be(user.Name);
        dto.ProfilePictureUrl.Should().Be(user.ProfilePictureUrl);
        dto.City.Should().Be(user.City);
        dto.State.Should().Be(user.State);
        dto.AccountType.Should().Be(UserType.Person);
        dto.Description.Should().BeEmpty();
        dto.Cnpj.Should().BeEmpty();

        dto.GetType().GetProperty("Email").Should().BeNull();
        dto.GetType().GetProperty("PhoneNumber").Should().BeNull();
        dto.GetType().GetProperty("Neighborhood").Should().BeNull();
        dto.GetType().GetProperty("Street").Should().BeNull();
        dto.GetType().GetProperty("StreetNumber").Should().BeNull();
        dto.GetType().GetProperty("ZipCode").Should().BeNull();
    }

    [Fact]
    public void ToPublicResponseDto_IncludesShelterIdentityFields()
    {
        var user = CreateUser();
        user.AccountType = UserType.Shelter;
        user.Cnpj = TestConstants.Users.ValidCnpj;
        user.Description = TestConstants.Users.ShelterDescription;
        user.Name = TestConstants.Users.ShelterName;

        var dto = user.ToPublicResponseDto();

        dto.AccountType.Should().Be(UserType.Shelter);
        dto.Cnpj.Should().Be(TestConstants.Users.ValidCnpj);
        dto.Description.Should().Be(TestConstants.Users.ShelterDescription);
        dto.Name.Should().Be(TestConstants.Users.ShelterName);
    }

    [Fact]
    public void ToResponseDto_IncludesPrivateProfileAndAccountFields()
    {
        var user = CreateUser();
        user.AccountType = UserType.Shelter;
        user.Cnpj = TestConstants.Users.ValidCnpj;
        user.Description = TestConstants.Users.ShelterDescription;

        var dto = user.ToResponseDto();

        dto.Id.Should().Be(user.Id);
        dto.Name.Should().Be(user.Name);
        dto.Email.Should().Be(user.Email);
        dto.EmailVerified.Should().BeFalse();
        dto.ProfilePictureUrl.Should().Be(user.ProfilePictureUrl);
        dto.AccountType.Should().Be(UserType.Shelter);
        dto.Cnpj.Should().Be(TestConstants.Users.ValidCnpj);
        dto.Description.Should().Be(TestConstants.Users.ShelterDescription);
        dto.PhoneNumber.Should().Be(user.PhoneNumber);
        dto.City.Should().Be(user.City);
        dto.State.Should().Be(user.State);
        dto.Neighborhood.Should().Be(user.Neighborhood);
        dto.Street.Should().Be(user.Street);
        dto.StreetNumber.Should().Be(user.StreetNumber);
    }

    [Fact]
    public void ToResponseDto_IncludesEmailVerifiedFlag()
    {
        var user = CreateUser();
        user.EmailVerified = true;
        user.EmailVerifiedAt = DateTime.UtcNow;

        user.ToResponseDto().EmailVerified.Should().BeTrue();
    }

    [Fact]
    public void ToPublicResponseDto_SerializesWithoutPrivateFields()
    {
        var json = JsonSerializer.SerializeToElement(CreateUser().ToPublicResponseDto());
        var names = json.EnumerateObject()
            .Select(p => p.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        names
            .Should()
            .BeEquivalentTo(
                [
                    "Id",
                    "Name",
                    "ProfilePictureUrl",
                    "City",
                    "State",
                    "AccountType",
                    "Description",
                    "Cnpj",
                ]
            );
    }

    private static User CreateUser() =>
        new()
        {
            Id = TestConstants.Users.ValidId,
            Name = TestConstants.Users.Username,
            Email = TestConstants.Users.Email,
            PasswordHash = "hash",
            ProfilePictureUrl = "https://cdn.example.com/photo.jpg",
            PhoneNumber = TestConstants.Users.PhoneNumber,
            ZipCode = TestConstants.Users.ZipCode,
            State = TestConstants.Users.State,
            City = TestConstants.Users.City,
            Neighborhood = TestConstants.Users.Neighborhood,
            Street = TestConstants.Users.Street,
            StreetNumber = TestConstants.Users.StreetNumber,
        };
}
