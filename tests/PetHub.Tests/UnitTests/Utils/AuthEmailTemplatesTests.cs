using FluentAssertions;
using PetHub.API.Utils;

namespace PetHub.Tests.UnitTests.Utils;

public class AuthEmailTemplatesTests
{
    [Fact]
    public void ExtractToken_ReadsPlainTokenFromBody()
    {
        var token = "abc-token_value";
        var (text, _) = AuthEmailTemplates.Verification(
            "Ada",
            "http://x/verify-email?token=abc",
            token,
            24
        );

        AuthEmailTemplates.ExtractToken(text).Should().Be(token);
    }

    [Fact]
    public void ExtractToken_ReturnsNullWhenMarkerMissing()
    {
        AuthEmailTemplates.ExtractToken("hello there").Should().BeNull();
    }
}
