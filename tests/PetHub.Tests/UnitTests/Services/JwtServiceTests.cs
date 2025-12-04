using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using PetHub.API.Configuration;
using PetHub.API.Services;

namespace PetHub.Tests.UnitTests.Services;

/// <summary>
/// Unit tests for JwtService
/// Tests token generation, claims, expiration, and signature validation
/// </summary>
public class JwtServiceTests
{
    private readonly JwtSettings _jwtSettings;
    private readonly JwtService _jwtService;
    private readonly JwtSecurityTokenHandler _tokenHandler;

    public JwtServiceTests()
    {
        _jwtSettings = new JwtSettings
        {
            SecretKey = TestConstants.Jwt.SecretKey,
            Issuer = TestConstants.Jwt.Issuer,
            Audience = TestConstants.Jwt.Audience,
            ExpirationMinutes = TestConstants.Jwt.ExpirationMinutes,
        };

        var options = Options.Create(_jwtSettings);
        _jwtService = new JwtService(options);
        _tokenHandler = new JwtSecurityTokenHandler();
    }

    #region Token Generation

    [Fact]
    public void GenerateToken_ReturnsValidJwtToken()
    {
        // Arrange
        var userId = TestConstants.Users.ValidUserId;
        var email = TestConstants.Users.ValidEmail;

        // Act
        var token = _jwtService.GenerateToken(userId, email);

        // Assert
        token.Should().NotBeNullOrEmpty();
        _tokenHandler.CanReadToken(token).Should().BeTrue();
    }

    [Fact]
    public void GenerateToken_ContainsAllRequiredClaims()
    {
        // Arrange
        var userId = TestConstants.Users.ValidUserId;
        var email = TestConstants.Users.ValidEmail;

        // Act
        var token = _jwtService.GenerateToken(userId, email);
        var jwtToken = _tokenHandler.ReadJwtToken(token);

        // Assert
        jwtToken
            .Claims.Should()
            .Contain(c => c.Type == ClaimTypes.NameIdentifier && c.Value == userId.ToString());
        jwtToken
            .Claims.Should()
            .Contain(c => c.Type == JwtRegisteredClaimNames.Sub && c.Value == userId.ToString());
        jwtToken
            .Claims.Should()
            .Contain(c => c.Type == JwtRegisteredClaimNames.Email && c.Value == email);
        jwtToken.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Jti);
    }

    [Fact]
    public void GenerateToken_JtiClaimIsUnique()
    {
        // Arrange
        var userId = TestConstants.Users.ValidUserId;
        var email = TestConstants.Users.ValidEmail;

        // Act
        var token1 = _jwtService.GenerateToken(userId, email);
        var token2 = _jwtService.GenerateToken(userId, email);

        var jwtToken1 = _tokenHandler.ReadJwtToken(token1);
        var jwtToken2 = _tokenHandler.ReadJwtToken(token2);

        var jti1 = jwtToken1.Claims.First(c => c.Type == JwtRegisteredClaimNames.Jti).Value;
        var jti2 = jwtToken2.Claims.First(c => c.Type == JwtRegisteredClaimNames.Jti).Value;

        // Assert
        jti1.Should().NotBe(jti2, "each token should have a unique JTI");
    }

    #endregion

    #region Token Properties

    [Theory]
    [InlineData("Issuer")]
    [InlineData("Audience")]
    [InlineData("Algorithm")]
    public void GenerateToken_HasCorrectTokenProperties(string propertyToTest)
    {
        // Arrange
        var userId = TestConstants.Users.ValidUserId;
        var email = TestConstants.Users.ValidEmail;

        // Act
        var token = _jwtService.GenerateToken(userId, email);
        var jwtToken = _tokenHandler.ReadJwtToken(token);

        // Assert
        switch (propertyToTest)
        {
            case "Issuer":
                jwtToken.Issuer.Should().Be(_jwtSettings.Issuer);
                break;
            case "Audience":
                jwtToken.Audiences.Should().Contain(_jwtSettings.Audience);
                break;
            case "Algorithm":
                jwtToken.SignatureAlgorithm.Should().Be(SecurityAlgorithms.HmacSha256);
                break;
        }
    }

    [Fact]
    public void GenerateToken_HasCorrectExpiration()
    {
        // Arrange
        var userId = TestConstants.Users.ValidUserId;
        var email = TestConstants.Users.ValidEmail;
        var beforeGeneration = DateTime.UtcNow;

        // Act
        var token = _jwtService.GenerateToken(userId, email);
        var jwtToken = _tokenHandler.ReadJwtToken(token);
        var afterGeneration = DateTime.UtcNow;

        // Assert
        var expectedExpiration = beforeGeneration.AddMinutes(_jwtSettings.ExpirationMinutes);
        jwtToken.ValidTo.Should().BeCloseTo(expectedExpiration, TimeSpan.FromSeconds(5));
        jwtToken.ValidTo.Should().BeAfter(afterGeneration);
    }

    #endregion

    #region Token Validation

    [Fact]
    public void GenerateToken_CanBeValidatedWithCorrectKey()
    {
        // Arrange
        var userId = TestConstants.Users.ValidUserId;
        var email = TestConstants.Users.ValidEmail;
        var token = _jwtService.GenerateToken(userId, email);

        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = _jwtSettings.Issuer,
            ValidAudience = _jwtSettings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(_jwtSettings.SecretKey)
            ),
        };

        // Act
        var principal = _tokenHandler.ValidateToken(
            token,
            validationParameters,
            out var validatedToken
        );

        // Assert
        validatedToken.Should().NotBeNull();
        principal.Should().NotBeNull();
        principal
            .Claims.Should()
            .Contain(c => c.Type == ClaimTypes.NameIdentifier && c.Value == userId.ToString());
    }

    [Fact]
    public void GenerateToken_FailsValidationWithWrongKey()
    {
        // Arrange
        var userId = TestConstants.Users.ValidUserId;
        var email = TestConstants.Users.ValidEmail;
        var token = _jwtService.GenerateToken(userId, email);

        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = _jwtSettings.Issuer,
            ValidAudience = _jwtSettings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(TestConstants.Jwt.WrongSecretKey)
            ),
        };

        // Act
        Action act = () => _tokenHandler.ValidateToken(token, validationParameters, out _);

        // Assert
        act.Should().Throw<SecurityTokenSignatureKeyNotFoundException>();
    }

    #endregion

    #region User ID and Email Claims

    [Theory]
    [InlineData("UserId")]
    [InlineData("Email")]
    public void GenerateToken_ClaimsMatchInput(string claimToTest)
    {
        // Arrange
        var userId = TestConstants.Users.ValidUserId;
        var email = TestConstants.Users.AnotherEmail;

        // Act
        var token = _jwtService.GenerateToken(userId, email);
        var jwtToken = _tokenHandler.ReadJwtToken(token);

        // Assert
        switch (claimToTest)
        {
            case "UserId":
                var nameIdentifierClaim = jwtToken
                    .Claims.First(c => c.Type == ClaimTypes.NameIdentifier)
                    .Value;
                var subClaim = jwtToken
                    .Claims.First(c => c.Type == JwtRegisteredClaimNames.Sub)
                    .Value;
                nameIdentifierClaim.Should().Be(userId.ToString());
                subClaim.Should().Be(userId.ToString());
                break;
            case "Email":
                var emailClaim = jwtToken
                    .Claims.First(c => c.Type == JwtRegisteredClaimNames.Email)
                    .Value;
                emailClaim.Should().Be(email);
                break;
        }
    }

    [Theory]
    [InlineData(TestConstants.Users.ValidEmail)]
    [InlineData(TestConstants.Users.EmailWithDots)]
    [InlineData(TestConstants.Users.EmailWithOrg)]
    public void GenerateToken_HandlesVariousEmailFormats(string email)
    {
        // Arrange
        var userId = TestConstants.Users.ValidUserId;

        // Act
        var token = _jwtService.GenerateToken(userId, email);
        var jwtToken = _tokenHandler.ReadJwtToken(token);

        var emailClaim = jwtToken.Claims.First(c => c.Type == JwtRegisteredClaimNames.Email).Value;

        // Assert
        emailClaim.Should().Be(email);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void GenerateToken_WithEmptyGuid_StillGeneratesValidToken()
    {
        // Arrange
        var userId = TestConstants.Users.EmptyUserId;
        var email = TestConstants.Users.ValidEmail;

        // Act
        var token = _jwtService.GenerateToken(userId, email);
        var jwtToken = _tokenHandler.ReadJwtToken(token);

        // Assert
        token.Should().NotBeNullOrEmpty();
        jwtToken
            .Claims.Should()
            .Contain(c => c.Type == ClaimTypes.NameIdentifier && c.Value == Guid.Empty.ToString());
    }

    [Fact]
    public void GenerateToken_WithEmptyEmail_StillGeneratesValidToken()
    {
        // Arrange
        var userId = TestConstants.Users.ValidUserId;
        var email = TestConstants.Users.EmptyEmail;

        // Act
        var token = _jwtService.GenerateToken(userId, email);
        var jwtToken = _tokenHandler.ReadJwtToken(token);

        // Assert
        token.Should().NotBeNullOrEmpty();
        jwtToken
            .Claims.Should()
            .Contain(c => c.Type == JwtRegisteredClaimNames.Email && c.Value == "");
    }

    [Fact]
    public void GenerateToken_MultipleCallsWithSameData_GenerateDifferentTokens()
    {
        // Arrange
        var userId = TestConstants.Users.ValidUserId;
        var email = TestConstants.Users.ValidEmail;

        // Act
        var token1 = _jwtService.GenerateToken(userId, email);
        var token2 = _jwtService.GenerateToken(userId, email);

        // Assert
        token1.Should().NotBe(token2, "tokens should differ due to different JTI and issue time");
    }

    #endregion
}
