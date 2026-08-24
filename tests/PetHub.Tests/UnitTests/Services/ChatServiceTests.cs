using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using PetHub.API.Data;
using PetHub.API.Enums;
using PetHub.API.Models;
using PetHub.API.Services;

namespace PetHub.Tests.UnitTests.Services;

public class ChatServiceTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly ChatService _service;
    private readonly User _owner;
    private readonly User _adopter;
    private readonly User _stranger;
    private readonly Pet _pet;

    public ChatServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new AppDbContext(options);
        _service = new ChatService(_context);

        _owner = CreateUser("Owner");
        _adopter = CreateUser("Adopter");
        _stranger = CreateUser("Stranger");

        var species = new Species { Name = TestConstants.SpeciesAndBreeds.DogName };
        var breed = new Breed
        {
            Name = TestConstants.SpeciesAndBreeds.LabradorName,
            Species = species,
        };

        _pet = new Pet
        {
            Name = TestConstants.Pets.Rex,
            Gender = PetGender.Male,
            Size = PetSize.Medium,
            AgeInMonths = TestConstants.Pets.ValidAgeInMonths,
            Description = TestConstants.Pets.DefaultDescription,
            UserId = _owner.Id,
            Species = species,
            Breed = breed,
        };

        _context.Users.AddRange(_owner, _adopter, _stranger);
        _context.Species.Add(species);
        _context.Breeds.Add(breed);
        _context.Pets.Add(_pet);
        _context.SaveChanges();
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task GetOrCreateForPetAsync_CreatesConversationBetweenAdopterAndOwner()
    {
        var (conversation, created) = await _service.GetOrCreateForPetAsync(_pet.Id, _adopter.Id);

        created.Should().BeTrue();
        conversation.PetId.Should().Be(_pet.Id);
        conversation.PetName.Should().Be(_pet.Name);
        conversation.OtherParticipant.Id.Should().Be(_owner.Id);
        conversation.OtherParticipant.Name.Should().Be(_owner.Name);
        conversation.UnreadCount.Should().Be(0);
        conversation.LastMessage.Should().BeNull();
    }

    [Fact]
    public async Task GetOrCreateForPetAsync_WhenCalledTwice_ReturnsSameConversation()
    {
        var first = await _service.GetOrCreateForPetAsync(_pet.Id, _adopter.Id);
        var second = await _service.GetOrCreateForPetAsync(_pet.Id, _adopter.Id);

        first.Created.Should().BeTrue();
        second.Created.Should().BeFalse();
        second.Conversation.Id.Should().Be(first.Conversation.Id);
        _context.Conversations.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetOrCreateForPetAsync_ForOwnPet_ThrowsArgumentException()
    {
        var act = async () => await _service.GetOrCreateForPetAsync(_pet.Id, _owner.Id);

        await act.Should()
            .ThrowAsync<ArgumentException>()
            .WithMessage("*own pet*");
    }

    [Fact]
    public async Task GetOrCreateForPetAsync_WhenPetDoesNotExist_ThrowsKeyNotFound()
    {
        var act = async () =>
            await _service.GetOrCreateForPetAsync(TestConstants.NonExistentIds.Generic, _adopter.Id);

        await act.Should().ThrowAsync<KeyNotFoundException>().WithMessage("*Pet*");
    }

    [Fact]
    public async Task GetOrCreateForAdoptionRequestAsync_CreatesThreadAndSeedsRequestMessage()
    {
        var request = await CreateAdoptionRequestAsync();

        var (conversation, created) = await _service.GetOrCreateForAdoptionRequestAsync(
            request.Id,
            _adopter.Id
        );

        created.Should().BeTrue();
        conversation.AdoptionRequestId.Should().Be(request.Id);
        conversation.PetId.Should().Be(_pet.Id);
        conversation.LastMessage.Should().NotBeNull();
        conversation.LastMessage!.Content.Should().Be(request.Message);
        conversation.LastMessage.SenderId.Should().Be(_adopter.Id);
        conversation.OtherParticipant.Id.Should().Be(_owner.Id);
    }

    [Fact]
    public async Task GetOrCreateForAdoptionRequestAsync_AsOwner_ShowsAdopterAsOtherParticipant()
    {
        var request = await CreateAdoptionRequestAsync();

        var (conversation, _) = await _service.GetOrCreateForAdoptionRequestAsync(
            request.Id,
            _owner.Id
        );

        conversation.OtherParticipant.Id.Should().Be(_adopter.Id);
        conversation.OtherParticipant.Name.Should().Be(_adopter.Name);
    }

    [Fact]
    public async Task GetOrCreateForAdoptionRequestAsync_LinksExistingPetConversation()
    {
        var existing = await _service.GetOrCreateForPetAsync(_pet.Id, _adopter.Id);
        var request = await CreateAdoptionRequestAsync();

        var (conversation, created) = await _service.GetOrCreateForAdoptionRequestAsync(
            request.Id,
            _adopter.Id
        );

        created.Should().BeFalse();
        conversation.Id.Should().Be(existing.Conversation.Id);
        conversation.AdoptionRequestId.Should().Be(request.Id);
        _context.Conversations.Should().HaveCount(1);
        _context.ChatMessages.Should().BeEmpty();
    }

    [Fact]
    public async Task GetOrCreateForAdoptionRequestAsync_WhenStranger_ThrowsUnauthorized()
    {
        var request = await CreateAdoptionRequestAsync();

        var act = async () =>
            await _service.GetOrCreateForAdoptionRequestAsync(request.Id, _stranger.Id);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task GetOrCreateForAdoptionRequestAsync_WhenMissing_ThrowsKeyNotFound()
    {
        var act = async () =>
            await _service.GetOrCreateForAdoptionRequestAsync(
                TestConstants.NonExistentIds.Generic,
                _adopter.Id
            );

        await act.Should().ThrowAsync<KeyNotFoundException>().WithMessage("*Adoption request*");
    }

    [Fact]
    public async Task SendMessageAsync_PersistsMessageAndUpdatesLastActivity()
    {
        var (conversation, _) = await _service.GetOrCreateForPetAsync(_pet.Id, _adopter.Id);

        var message = await _service.SendMessageAsync(
            conversation.Id,
            _adopter.Id,
            $"  {TestConstants.Chat.ValidMessage}  "
        );

        message.Content.Should().Be(TestConstants.Chat.ValidMessage);
        message.SenderId.Should().Be(_adopter.Id);
        message.SenderName.Should().Be(_adopter.Name);
        message.IsRead.Should().BeFalse();
        message.ConversationId.Should().Be(conversation.Id);

        var stored = await _context.Conversations.FirstAsync(c => c.Id == conversation.Id);
        stored.LastMessageAt.Should().Be(message.SentAt);
    }

    [Fact]
    public async Task SendMessageAsync_WhenEmpty_ThrowsArgumentException()
    {
        var (conversation, _) = await _service.GetOrCreateForPetAsync(_pet.Id, _adopter.Id);

        var act = async () => await _service.SendMessageAsync(conversation.Id, _adopter.Id, "   ");

        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*empty*");
    }

    [Fact]
    public async Task SendMessageAsync_WhenTooLong_ThrowsArgumentException()
    {
        var (conversation, _) = await _service.GetOrCreateForPetAsync(_pet.Id, _adopter.Id);
        var content = new string('a', ChatService.MaxMessageContentLength + 1);

        var act = async () => await _service.SendMessageAsync(conversation.Id, _adopter.Id, content);

        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*2000*");
    }

    [Fact]
    public async Task SendMessageAsync_WhenNotParticipant_ThrowsUnauthorized()
    {
        var (conversation, _) = await _service.GetOrCreateForPetAsync(_pet.Id, _adopter.Id);

        var act = async () =>
            await _service.SendMessageAsync(
                conversation.Id,
                _stranger.Id,
                TestConstants.Chat.ValidMessage
            );

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task GetInboxAsync_ReturnsOnlyCurrentUserConversationsNewestFirst()
    {
        var first = await _service.GetOrCreateForPetAsync(_pet.Id, _adopter.Id);
        await _service.SendMessageAsync(
            first.Conversation.Id,
            _adopter.Id,
            TestConstants.Chat.ValidMessage
        );

        var otherPet = await CreateSecondPetAsync();
        var second = await _service.GetOrCreateForPetAsync(otherPet.Id, _adopter.Id);
        await _service.SendMessageAsync(
            second.Conversation.Id,
            _adopter.Id,
            TestConstants.Chat.SecondMessage
        );

        var strangerThread = await _service.GetOrCreateForPetAsync(otherPet.Id, _stranger.Id);
        await _service.SendMessageAsync(
            strangerThread.Conversation.Id,
            _stranger.Id,
            TestConstants.Chat.ValidMessage
        );

        var inbox = await _service.GetInboxAsync(_adopter.Id);

        inbox.Should().HaveCount(2);
        inbox[0].Id.Should().Be(second.Conversation.Id);
        inbox[1].Id.Should().Be(first.Conversation.Id);
        inbox.Should().OnlyContain(c => c.OtherParticipant.Id == _owner.Id);
    }

    [Fact]
    public async Task GetInboxAsync_WhenEmpty_ReturnsEmptyList()
    {
        var inbox = await _service.GetInboxAsync(_adopter.Id);

        inbox.Should().BeEmpty();
    }

    [Fact]
    public async Task GetMessagesAsync_ReturnsChronologicalPageAndHonorsCursor()
    {
        var (conversation, _) = await _service.GetOrCreateForPetAsync(_pet.Id, _adopter.Id);
        var first = await _service.SendMessageAsync(
            conversation.Id,
            _adopter.Id,
            TestConstants.Chat.ValidMessage
        );
        var second = await _service.SendMessageAsync(
            conversation.Id,
            _owner.Id,
            TestConstants.Chat.OwnerReply
        );
        var third = await _service.SendMessageAsync(
            conversation.Id,
            _adopter.Id,
            TestConstants.Chat.SecondMessage
        );

        var latest = await _service.GetMessagesAsync(conversation.Id, _adopter.Id, null, 2);
        latest.Select(m => m.Id).Should().Equal(second.Id, third.Id);

        var older = await _service.GetMessagesAsync(conversation.Id, _adopter.Id, second.Id, 2);
        older.Select(m => m.Id).Should().Equal(first.Id);
    }

    [Fact]
    public async Task MarkAsReadAsync_MarksOnlyMessagesFromTheOtherParticipant()
    {
        var (conversation, _) = await _service.GetOrCreateForPetAsync(_pet.Id, _adopter.Id);
        await _service.SendMessageAsync(
            conversation.Id,
            _adopter.Id,
            TestConstants.Chat.ValidMessage
        );
        await _service.SendMessageAsync(
            conversation.Id,
            _owner.Id,
            TestConstants.Chat.OwnerReply
        );

        var before = await _service.GetConversationAsync(conversation.Id, _adopter.Id);
        before.UnreadCount.Should().Be(1);

        var result = await _service.MarkAsReadAsync(conversation.Id, _adopter.Id);

        result.MarkedCount.Should().Be(1);
        var after = await _service.GetConversationAsync(conversation.Id, _adopter.Id);
        after.UnreadCount.Should().Be(0);

        var ownerView = await _service.GetConversationAsync(conversation.Id, _owner.Id);
        ownerView.UnreadCount.Should().Be(1);
    }

    [Fact]
    public async Task GetConversationAsync_WhenMissing_ThrowsKeyNotFound()
    {
        var act = async () =>
            await _service.GetConversationAsync(TestConstants.NonExistentIds.Generic, _adopter.Id);

        await act.Should().ThrowAsync<KeyNotFoundException>().WithMessage("*Conversation*");
    }

    [Fact]
    public async Task IsParticipantAsync_ReturnsExpectedMembership()
    {
        var (conversation, _) = await _service.GetOrCreateForPetAsync(_pet.Id, _adopter.Id);

        (await _service.IsParticipantAsync(conversation.Id, _adopter.Id)).Should().BeTrue();
        (await _service.IsParticipantAsync(conversation.Id, _owner.Id)).Should().BeTrue();
        (await _service.IsParticipantAsync(conversation.Id, _stranger.Id)).Should().BeFalse();
        (await _service.IsParticipantAsync(TestConstants.NonExistentIds.Generic, _adopter.Id))
            .Should()
            .BeFalse();
    }

    private async Task<AdoptionRequest> CreateAdoptionRequestAsync()
    {
        var request = new AdoptionRequest
        {
            PetId = _pet.Id,
            AdopterId = _adopter.Id,
            Message = TestConstants.Chat.AdoptionRequestMessage,
            Status = AdoptionStatus.Pending,
            CreatedAt = DateTime.UtcNow,
        };
        _context.AdoptionRequests.Add(request);
        await _context.SaveChangesAsync();
        return request;
    }

    private async Task<Pet> CreateSecondPetAsync()
    {
        var pet = new Pet
        {
            Name = TestConstants.Pets.Luna,
            Gender = PetGender.Female,
            Size = PetSize.Small,
            AgeInMonths = 12,
            Description = TestConstants.Pets.ShortDescription,
            UserId = _owner.Id,
            SpeciesId = _pet.SpeciesId,
            BreedId = _pet.BreedId,
        };
        _context.Pets.Add(pet);
        await _context.SaveChangesAsync();
        return pet;
    }

    private static User CreateUser(string name)
    {
        return new User
        {
            Id = Guid.NewGuid(),
            Name = name,
            Email = $"{name.ToLowerInvariant()}@example.com",
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
