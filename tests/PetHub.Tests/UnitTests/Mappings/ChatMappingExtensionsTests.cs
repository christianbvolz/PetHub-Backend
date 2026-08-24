using FluentAssertions;
using PetHub.API.Mappings;
using PetHub.API.Models;

namespace PetHub.Tests.UnitTests.Mappings;

public class ChatMappingExtensionsTests
{
    [Fact]
    public void ToResponseDto_ExposesPublicParticipantAndOmitsPrivateContact()
    {
        var owner = CreateUser("Owner", "owner@example.com");
        var adopter = CreateUser("Adopter", "adopter@example.com");
        var conversation = new Conversation
        {
            Id = 1,
            UserAId = owner.Id,
            UserA = owner,
            UserBId = adopter.Id,
            UserB = adopter,
            PetId = 10,
            Pet = new Pet { Id = 10, Name = TestConstants.Pets.Rex },
            LastMessageAt = DateTime.UtcNow,
        };

        var dto = conversation.ToResponseDto(adopter.Id);

        dto.OtherParticipant.Id.Should().Be(owner.Id);
        dto.OtherParticipant.Name.Should().Be(owner.Name);
        dto.OtherParticipant.GetType().GetProperty("Email").Should().BeNull();
        dto.OtherParticipant.GetType().GetProperty("PhoneNumber").Should().BeNull();
        dto.OtherParticipant.GetType().GetProperty("Street").Should().BeNull();
        dto.PetName.Should().Be(TestConstants.Pets.Rex);
    }

    [Fact]
    public void MessageToResponseDto_UsesPersistedSenderName()
    {
        var sender = CreateUser("Adopter", "adopter@example.com");
        var message = new ChatMessage
        {
            Id = 5,
            ConversationId = 1,
            SenderId = sender.Id,
            Sender = sender,
            Content = TestConstants.Chat.ValidMessage,
            SentAt = DateTime.UtcNow,
            IsRead = false,
        };

        var dto = message.ToResponseDto();

        dto.SenderId.Should().Be(sender.Id);
        dto.SenderName.Should().Be(sender.Name);
        dto.Content.Should().Be(TestConstants.Chat.ValidMessage);
    }

    private static User CreateUser(string name, string email)
    {
        return new User
        {
            Id = Guid.NewGuid(),
            Name = name,
            Email = email,
            PasswordHash = "hash",
            PhoneNumber = TestConstants.Users.PhoneNumber,
            ZipCode = TestConstants.Users.ZipCode,
            State = TestConstants.Users.State,
            City = TestConstants.Users.City,
            Neighborhood = TestConstants.Users.Neighborhood,
            Street = TestConstants.Users.Street,
            StreetNumber = TestConstants.Users.StreetNumber,
        };
    }
}
