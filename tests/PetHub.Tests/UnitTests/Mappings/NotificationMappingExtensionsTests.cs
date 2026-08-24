using FluentAssertions;
using PetHub.API.Enums;
using PetHub.API.Mappings;
using PetHub.API.Models;

namespace PetHub.Tests.UnitTests.Mappings;

public class NotificationMappingExtensionsTests
{
    [Fact]
    public void ToResponseDto_CopiesPersistedFields()
    {
        var createdAt = DateTime.UtcNow;
        var notification = new Notification
        {
            Id = 7,
            UserId = Guid.NewGuid(),
            Type = NotificationType.AdoptionRequestCreated,
            Title = "New adoption request",
            Message = "Adopter requested to adopt Rex.",
            AdoptionRequestId = 3,
            PetId = 11,
            PetName = TestConstants.Pets.Rex,
            IsRead = false,
            CreatedAt = createdAt,
        };

        var dto = notification.ToResponseDto();

        dto.Id.Should().Be(7);
        dto.Type.Should().Be(NotificationType.AdoptionRequestCreated);
        dto.Title.Should().Be(notification.Title);
        dto.Message.Should().Be(notification.Message);
        dto.AdoptionRequestId.Should().Be(3);
        dto.PetId.Should().Be(11);
        dto.PetName.Should().Be(TestConstants.Pets.Rex);
        dto.IsRead.Should().BeFalse();
        dto.CreatedAt.Should().Be(createdAt);
    }
}
