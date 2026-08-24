using FluentAssertions;
using PetHub.API.Utils;

namespace PetHub.Tests.UnitTests.Utils;

public class CnpjHelperTests
{
    [Theory]
    [InlineData(TestConstants.Users.ValidCnpj)]
    [InlineData(TestConstants.Users.ValidFormattedCnpj)]
    [InlineData(TestConstants.Users.AnotherValidCnpj)]
    public void IsValid_WithValidCnpj_ReturnsTrue(string cnpj)
    {
        CnpjHelper.IsValid(cnpj).Should().BeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("123")]
    [InlineData("00000000000000")]
    [InlineData(TestConstants.Users.InvalidCnpj)]
    public void IsValid_WithInvalidCnpj_ReturnsFalse(string? cnpj)
    {
        CnpjHelper.IsValid(cnpj).Should().BeFalse();
    }

    [Fact]
    public void Normalize_StripsFormattingAndKeepsDigits()
    {
        CnpjHelper.Normalize(TestConstants.Users.ValidFormattedCnpj)
            .Should()
            .Be(TestConstants.Users.ValidCnpj);
    }

    [Fact]
    public void Normalize_WithNullOrWhitespace_ReturnsEmpty()
    {
        CnpjHelper.Normalize(null).Should().BeEmpty();
        CnpjHelper.Normalize("   ").Should().BeEmpty();
    }
}
