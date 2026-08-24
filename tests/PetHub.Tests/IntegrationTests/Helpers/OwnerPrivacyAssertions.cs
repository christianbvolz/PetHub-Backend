using System.Text.Json;
using FluentAssertions;

namespace PetHub.Tests.IntegrationTests.Helpers;

public static class OwnerPrivacyAssertions
{
    private static readonly string[] PrivateOwnerFields =
    [
        "email",
        "phoneNumber",
        "neighborhood",
        "street",
        "streetNumber",
        "zipCode",
    ];

    public static void ShouldExposeOnlyPublicOwnerFields(JsonElement owner)
    {
        foreach (var field in PrivateOwnerFields)
        {
            owner
                .TryGetProperty(field, out _)
                .Should()
                .BeFalse($"public owner payload must not include '{field}'");
        }

        owner.GetProperty("id").GetGuid().Should().NotBe(Guid.Empty);
        owner.GetProperty("name").GetString().Should().NotBeNullOrEmpty();
        owner.GetProperty("city").GetString().Should().NotBeNullOrEmpty();
        owner.GetProperty("state").GetString().Should().NotBeNullOrEmpty();
        owner.TryGetProperty("profilePictureUrl", out _).Should().BeTrue();
        owner.TryGetProperty("accountType", out var accountType).Should().BeTrue();
        accountType.GetString().Should().NotBeNullOrEmpty();
        owner.TryGetProperty("description", out _).Should().BeTrue();
        owner.TryGetProperty("cnpj", out _).Should().BeTrue();
    }
}
