using FluentAssertions;
using PetHub.API.Utils;

namespace PetHub.Tests.UnitTests.Utils;

public class PasswordHelperTests
{
    [Fact]
    public void HashPassword_WithValidPassword_ReturnsNonEmptyHash()
    {
        // Arrange
        var password = TestConstants.Passwords.ValidPassword;

        // Act
        var hash = PasswordHelper.HashPassword(password);

        // Assert
        hash.Should().NotBeNullOrEmpty();
        hash.Should().NotBe(password); // Hash should be different from plain password
    }

    [Fact]
    public void HashPassword_WithSamePassword_ReturnsDifferentHashes()
    {
        // Arrange
        var password = TestConstants.Passwords.ValidPassword;

        // Act
        var hash1 = PasswordHelper.HashPassword(password);
        var hash2 = PasswordHelper.HashPassword(password);

        // Assert
        hash1.Should().NotBe(hash2); // BCrypt uses salt, so hashes should be different
    }

    [Fact]
    public void HashPassword_WithDifferentPasswords_ReturnsDifferentHashes()
    {
        // Arrange
        var password1 = TestConstants.Passwords.AnotherValidPassword;
        var password2 = TestConstants.Passwords.DifferentPassword;

        // Act
        var hash1 = PasswordHelper.HashPassword(password1);
        var hash2 = PasswordHelper.HashPassword(password2);

        // Assert
        hash1.Should().NotBe(hash2);
    }

    [Fact]
    public void VerifyPassword_WithCorrectPassword_ReturnsTrue()
    {
        // Arrange
        var password = TestConstants.Passwords.ValidPassword;
        var hash = PasswordHelper.HashPassword(password);

        // Act
        var result = PasswordHelper.VerifyPassword(password, hash);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void VerifyPassword_WithIncorrectPassword_ReturnsFalse()
    {
        // Arrange
        var correctPassword = TestConstants.Passwords.ValidPassword;
        var incorrectPassword = TestConstants.Passwords.WrongPassword;
        var hash = PasswordHelper.HashPassword(correctPassword);

        // Act
        var result = PasswordHelper.VerifyPassword(incorrectPassword, hash);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void VerifyPassword_WithCaseSensitivePassword_ReturnsFalse()
    {
        // Arrange
        var correctPassword = TestConstants.Passwords.ValidPassword;
        var wrongCasePassword = TestConstants.Passwords.WrongCasePassword;
        var hash = PasswordHelper.HashPassword(correctPassword);

        // Act
        var result = PasswordHelper.VerifyPassword(wrongCasePassword, hash);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void VerifyPassword_WithMalformedHash_ThrowsException()
    {
        // Arrange
        var password = TestConstants.Passwords.ValidPassword;
        var malformedHash = TestConstants.Passwords.MalformedHash;

        // Act
        Action act = () => PasswordHelper.VerifyPassword(password, malformedHash);

        // Assert
        act.Should().Throw<Exception>(); // BCrypt will throw when hash format is invalid
    }

    [Theory]
    [InlineData(TestConstants.Passwords.ShortPassword)]
    [InlineData(TestConstants.Passwords.AveragePassword)]
    [InlineData(TestConstants.Passwords.LongPassword)]
    public void HashPassword_WithVariousLengths_ReturnsValidHash(string password)
    {
        // Act
        var hash = PasswordHelper.HashPassword(password);

        // Assert
        hash.Should().NotBeNullOrEmpty();
        hash.Should().StartWith("$2"); // BCrypt hashes start with version identifier
    }

    [Fact]
    public void VerifyPassword_WithSpecialCharacters_WorksCorrectly()
    {
        // Arrange
        var password = TestConstants.Passwords.SpecialCharsPassword; // Emojis, special chars and Japanese
        var hash = PasswordHelper.HashPassword(password);

        // Act
        var result = PasswordHelper.VerifyPassword(password, hash);

        // Assert
        result.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("   ")]
    [InlineData("\t")]
    [InlineData("\n")]
    public void HashPassword_WithInvalidPassword_ThrowsArgumentException(string invalidPassword)
    {
        // Act
        Action act = () => PasswordHelper.HashPassword(invalidPassword);

        // Assert
        act.Should()
            .Throw<ArgumentException>()
            .WithMessage("*Password cannot be null, empty or whitespace*")
            .And.ParamName.Should()
            .Be("password");
    }

    [Fact]
    public void HashPassword_WithNullPassword_ThrowsArgumentException()
    {
        // Act
        Action act = () => PasswordHelper.HashPassword(null!);

        // Assert
        act.Should()
            .Throw<ArgumentException>()
            .WithMessage("*Password cannot be null, empty or whitespace*")
            .And.ParamName.Should()
            .Be("password");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("   ")]
    public void VerifyPassword_WithInvalidPassword_ThrowsArgumentException(string invalidPassword)
    {
        // Arrange
        var validHash = PasswordHelper.HashPassword(TestConstants.Passwords.ValidPassword);

        // Act
        var act = () => PasswordHelper.VerifyPassword(invalidPassword, validHash);

        // Assert
        act.Should()
            .Throw<ArgumentException>()
            .WithMessage("*Password cannot be null, empty or whitespace*")
            .And.ParamName.Should()
            .Be("password");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("   ")]
    public void VerifyPassword_WithEmptyHash_ThrowsArgumentException(string emptyHash)
    {
        // Act
        Action act = () =>
            PasswordHelper.VerifyPassword(TestConstants.Passwords.ValidPassword, emptyHash);

        // Assert
        act.Should()
            .Throw<ArgumentException>()
            .WithMessage("*Password hash cannot be null, empty or whitespace*")
            .And.ParamName.Should()
            .Be("passwordHash");
    }

    [Fact]
    public void VerifyPassword_WithNullPassword_ThrowsArgumentException()
    {
        // Arrange
        var validHash = PasswordHelper.HashPassword(TestConstants.Passwords.ValidPassword);

        // Act
        Action act = () => PasswordHelper.VerifyPassword(null!, validHash);

        // Assert
        act.Should()
            .Throw<ArgumentException>()
            .WithMessage("*Password cannot be null, empty or whitespace*")
            .And.ParamName.Should()
            .Be("password");
    }

    [Fact]
    public void VerifyPassword_WithNullHash_ThrowsArgumentException()
    {
        // Act
        Action act = () =>
            PasswordHelper.VerifyPassword(TestConstants.Passwords.ValidPassword, null!);

        // Assert
        act.Should()
            .Throw<ArgumentException>()
            .WithMessage("*Password hash cannot be null, empty or whitespace*")
            .And.ParamName.Should()
            .Be("passwordHash");
    }
}
