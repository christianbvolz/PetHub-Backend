using FluentAssertions;
using PetHub.API.Utils;

namespace PetHub.Tests.UnitTests.Utils;

public class PasswordHelperTests
{
    [Fact]
    public void HashPassword_WithValidPassword_ReturnsNonEmptyHash()
    {
        // Arrange
        var password = "SecurePassword123!";

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
        var password = "SecurePassword123!";

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
        var password1 = "Password123!";
        var password2 = "DifferentPassword456!";

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
        var password = "SecurePassword123!";
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
        var correctPassword = "SecurePassword123!";
        var incorrectPassword = "WrongPassword456!";
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
        var correctPassword = "SecurePassword123!";
        var wrongCasePassword = "securepassword123!";
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
        var password = "SecurePassword123!";
        var malformedHash = "not-a-valid-bcrypt-hash";

        // Act
        Action act = () => PasswordHelper.VerifyPassword(password, malformedHash);

        // Assert
        act.Should().Throw<Exception>(); // BCrypt will throw when hash format is invalid
    }

    [Theory]
    [InlineData("short")]
    [InlineData("averageLengthPassword123")]
    [InlineData("VeryLongPasswordWithLotsOfCharacters123456789!@#$%^&*()")]
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
        var password = "🔒🔑Password123!こんにちは"; // Emojis, special chars and Japanese
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
        var validHash = PasswordHelper.HashPassword("ValidPassword123!");

        // Act
        Action act = () => PasswordHelper.VerifyPassword(invalidPassword, validHash);

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
        Action act = () => PasswordHelper.VerifyPassword("ValidPassword123!", emptyHash);

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
        var validHash = PasswordHelper.HashPassword("ValidPassword123!");

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
        Action act = () => PasswordHelper.VerifyPassword("ValidPassword123!", null!);

        // Assert
        act.Should()
            .Throw<ArgumentException>()
            .WithMessage("*Password hash cannot be null, empty or whitespace*")
            .And.ParamName.Should()
            .Be("passwordHash");
    }
}
