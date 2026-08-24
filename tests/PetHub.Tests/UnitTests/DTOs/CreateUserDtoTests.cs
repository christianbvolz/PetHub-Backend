using System.ComponentModel.DataAnnotations;
using FluentAssertions;
using PetHub.API.DTOs.User;
using PetHub.API.Enums;

namespace PetHub.Tests.UnitTests.DTOs;

public class CreateUserDtoTests
{
    [Fact]
    public void Validate_PersonWithoutCnpj_HasNoErrors()
    {
        var dto = TestConstants.DtoBuilders.CreateValidUserDto();

        var results = Validate(dto);

        results.Should().BeEmpty();
        dto.AccountType.Should().Be(UserType.Person);
    }

    [Fact]
    public void Validate_ShelterWithValidCnpj_HasNoErrors()
    {
        var dto = TestConstants.DtoBuilders.CreateValidShelterDto();

        var results = Validate(dto);

        results.Should().BeEmpty();
    }

    [Fact]
    public void Validate_ShelterWithoutCnpj_ReturnsError()
    {
        var dto = TestConstants.DtoBuilders.CreateValidUserDto(accountType: UserType.Shelter);

        var results = Validate(dto);

        results.Should().ContainSingle(r => r.ErrorMessage!.Contains("CNPJ"));
    }

    [Fact]
    public void Validate_ShelterWithInvalidCnpj_ReturnsError()
    {
        var dto = TestConstants.DtoBuilders.CreateValidShelterDto(
            cnpj: TestConstants.Users.InvalidCnpj
        );

        var results = Validate(dto);

        results.Should().ContainSingle(r => r.ErrorMessage!.Contains("CNPJ"));
    }

    [Fact]
    public void Validate_PersonWithCnpj_ReturnsError()
    {
        var dto = TestConstants.DtoBuilders.CreateValidUserDto(cnpj: TestConstants.Users.ValidCnpj);

        var results = Validate(dto);

        results.Should().ContainSingle(r =>
            r.ErrorMessage!.Contains("CNPJ is only allowed for shelter accounts.")
        );
    }

    private static List<ValidationResult> Validate(CreateUserDto dto) =>
        dto.Validate(new ValidationContext(dto)).ToList();
}
