using FluentAssertions;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using PetHub.API.Data;
using PetHub.API.Enums;
using PetHub.API.Hubs;
using PetHub.API.Models;
using PetHub.API.Services;

namespace PetHub.Tests.UnitTests.Services;

public class NotificationServiceTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly Mock<IClientProxy> _clientProxy;
    private readonly NotificationService _service;
    private readonly User _owner;
    private readonly User _adopter;
    private readonly Pet _pet;
    private readonly AdoptionRequest _request;

    public NotificationServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new AppDbContext(options);

        var hubContext = new Mock<IHubContext<NotificationHub>>();
        var clients = new Mock<IHubClients>();
        _clientProxy = new Mock<IClientProxy>();

        hubContext.Setup(h => h.Clients).Returns(clients.Object);
        clients.Setup(c => c.Group(It.IsAny<string>())).Returns(_clientProxy.Object);
        _clientProxy
            .Setup(p =>
                p.SendCoreAsync(
                    It.IsAny<string>(),
                    It.IsAny<object?[]>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .Returns(Task.CompletedTask);

        _service = new NotificationService(
            _context,
            hubContext.Object,
            NullLogger<NotificationService>.Instance
        );

        _owner = CreateUser("Owner");
        _adopter = CreateUser("Adopter");

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
            User = _owner,
            Species = species,
            Breed = breed,
        };

        _context.Users.AddRange(_owner, _adopter);
        _context.Species.Add(species);
        _context.Breeds.Add(breed);
        _context.Pets.Add(_pet);
        _context.SaveChanges();

        _request = new AdoptionRequest
        {
            PetId = _pet.Id,
            Pet = _pet,
            AdopterId = _adopter.Id,
            Adopter = _adopter,
            Message = TestConstants.Chat.AdoptionRequestMessage,
            Status = AdoptionStatus.Pending,
        };
        _context.AdoptionRequests.Add(_request);
        _context.SaveChanges();
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task NotifyAdoptionEventAsync_WhenRecipientIsEmpty_DoesNotPersist()
    {
        await _service.NotifyAdoptionEventAsync(
            NotificationType.AdoptionRequestCreated,
            Guid.Empty,
            _request
        );

        _context.Notifications.Should().BeEmpty();
        _clientProxy.Verify(
            p =>
                p.SendCoreAsync(
                    It.IsAny<string>(),
                    It.IsAny<object?[]>(),
                    It.IsAny<CancellationToken>()
                ),
            Times.Never
        );
    }

    [Fact]
    public async Task NotifyAdoptionEventAsync_Created_PersistsForOwnerAndPushesToHub()
    {
        await _service.NotifyAdoptionEventAsync(
            NotificationType.AdoptionRequestCreated,
            _owner.Id,
            _request
        );

        var notification = await _context.Notifications.SingleAsync();
        notification.UserId.Should().Be(_owner.Id);
        notification.Type.Should().Be(NotificationType.AdoptionRequestCreated);
        notification.AdoptionRequestId.Should().Be(_request.Id);
        notification.PetId.Should().Be(_pet.Id);
        notification.PetName.Should().Be(_pet.Name);
        notification.IsRead.Should().BeFalse();
        notification.Title.Should().Be("New adoption request");
        notification.Message.Should().Be("Adopter requested to adopt Rex.");

        _clientProxy.Verify(
            p =>
                p.SendCoreAsync(
                    NotificationHub.ReceiveNotificationEvent,
                    It.IsAny<object?[]>(),
                    It.IsAny<CancellationToken>()
                ),
            Times.Once
        );
    }

    [Theory]
    [InlineData(
        NotificationType.AdoptionRequestApproved,
        "Adoption request approved",
        "Your request to adopt Rex was approved."
    )]
    [InlineData(
        NotificationType.AdoptionRequestRejected,
        "Adoption request rejected",
        "Your request to adopt Rex was rejected."
    )]
    [InlineData(
        NotificationType.AdoptionRequestCancelled,
        "Adoption request cancelled",
        "Adopter cancelled the request to adopt Rex."
    )]
    public async Task NotifyAdoptionEventAsync_BuildsExpectedCopy(
        NotificationType type,
        string expectedTitle,
        string expectedMessage
    )
    {
        await _service.NotifyAdoptionEventAsync(type, _adopter.Id, _request);

        var notification = await _context.Notifications.SingleAsync();
        notification.Title.Should().Be(expectedTitle);
        notification.Message.Should().Be(expectedMessage);
        notification.Type.Should().Be(type);
    }

    [Fact]
    public async Task NotifyAdoptionEventAsync_WhenHubFails_StillPersistsNotification()
    {
        _clientProxy
            .Setup(p =>
                p.SendCoreAsync(
                    It.IsAny<string>(),
                    It.IsAny<object?[]>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ThrowsAsync(new HubException("disconnected"));

        var act = async () =>
            await _service.NotifyAdoptionEventAsync(
                NotificationType.AdoptionRequestCreated,
                _owner.Id,
                _request
            );

        await act.Should().NotThrowAsync();
        _context.Notifications.Should().ContainSingle();
    }

    [Fact]
    public async Task GetForUserAsync_ReturnsOnlyRecipientNotificationsNewestFirst()
    {
        await _service.NotifyAdoptionEventAsync(
            NotificationType.AdoptionRequestCreated,
            _owner.Id,
            _request
        );
        await _service.NotifyAdoptionEventAsync(
            NotificationType.AdoptionRequestApproved,
            _adopter.Id,
            _request
        );

        var ownerInbox = await _service.GetForUserAsync(_owner.Id);
        var adopterInbox = await _service.GetForUserAsync(_adopter.Id);

        ownerInbox.Should().ContainSingle();
        ownerInbox[0].Type.Should().Be(NotificationType.AdoptionRequestCreated);
        adopterInbox.Should().ContainSingle();
        adopterInbox[0].Type.Should().Be(NotificationType.AdoptionRequestApproved);
    }

    [Fact]
    public async Task MarkAsReadAsync_MarksOwnedNotificationAndIgnoresOthers()
    {
        await _service.NotifyAdoptionEventAsync(
            NotificationType.AdoptionRequestCreated,
            _owner.Id,
            _request
        );
        var stored = await _context.Notifications.SingleAsync();

        var owned = await _service.MarkAsReadAsync(stored.Id, _owner.Id);
        var foreign = await _service.MarkAsReadAsync(stored.Id, _adopter.Id);

        owned.Should().NotBeNull();
        owned!.IsRead.Should().BeTrue();
        foreign.Should().BeNull();
        (await _service.GetUnreadCountAsync(_owner.Id)).Should().Be(0);
    }

    [Fact]
    public async Task MarkAllAsReadAsync_MarksEveryUnreadNotificationForUser()
    {
        await _service.NotifyAdoptionEventAsync(
            NotificationType.AdoptionRequestCreated,
            _owner.Id,
            _request
        );
        await _service.NotifyAdoptionEventAsync(
            NotificationType.AdoptionRequestCancelled,
            _owner.Id,
            _request
        );

        var marked = await _service.MarkAllAsReadAsync(_owner.Id);

        marked.Should().Be(2);
        (await _service.GetUnreadCountAsync(_owner.Id)).Should().Be(0);
        (await _service.MarkAllAsReadAsync(_owner.Id)).Should().Be(0);
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
