using System.Net;
using FluentAssertions;
using PetHub.API.DTOs.AdoptionRequest;
using PetHub.API.DTOs.Notification;
using PetHub.API.Enums;
using PetHub.Tests.Extensions;
using PetHub.Tests.IntegrationTests.Helpers;
using PetHub.Tests.IntegrationTests.Infrastructure;

namespace PetHub.Tests.IntegrationTests.Controllers.NotificationsControllerTests;

public class NotificationsControllerTests : IClassFixture<PetHubWebApplicationFactory>, IAsyncLifetime
{
    private readonly PetHubWebApplicationFactory _factory;
    private readonly HttpClient _ownerClient;
    private readonly HttpClient _adopterClient;
    private readonly HttpClient _otherAdopterClient;
    private string _ownerToken = string.Empty;
    private string _adopterToken = string.Empty;
    private int _petId;

    public NotificationsControllerTests(PetHubWebApplicationFactory factory)
    {
        _factory = factory;
        _ownerClient = factory.CreateClient();
        _adopterClient = factory.CreateClient();
        _otherAdopterClient = factory.CreateClient();
    }

    public async Task InitializeAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<API.Data.AppDbContext>();
        await dbContext.Database.EnsureDeletedAsync();
        await dbContext.Database.EnsureCreatedAsync();
        await TestDataSeeder.SeedTestData(dbContext);

        _ownerToken = await AuthenticationHelper.RegisterAndGetTokenAsync(
            _ownerClient,
            email: "owner-notify@test.com"
        );
        _adopterToken = await AuthenticationHelper.RegisterAndGetTokenAsync(
            _adopterClient,
            email: "adopter-notify@test.com"
        );
        var otherToken = await AuthenticationHelper.RegisterAndGetTokenAsync(
            _otherAdopterClient,
            email: "other-adopter-notify@test.com"
        );

        _ownerClient.AddAuthToken(_ownerToken);
        _adopterClient.AddAuthToken(_adopterToken);
        _otherAdopterClient.AddAuthToken(otherToken);

        var petResponse = await _ownerClient.PostAsJsonAsync(
            TestConstants.ApiPaths.Pets,
            TestConstants.DtoBuilders.CreateValidPetDto()
        );
        petResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var pet = await petResponse.ReadApiResponseDataAsync<API.DTOs.Pet.PetResponseDto>();
        _petId = pet!.Id;
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task GetNotifications_WithoutToken_ReturnsUnauthorized()
    {
        var anonymous = _factory.CreateClient();

        var response = await anonymous.GetAsync(TestConstants.ApiPaths.Notifications);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateAdoptionRequest_NotifiesOwnerNotAdopter()
    {
        await CreateRequestAsync(_adopterClient);

        var ownerInbox = await GetNotificationsAsync(_ownerClient);
        var adopterInbox = await GetNotificationsAsync(_adopterClient);

        ownerInbox
            .Should()
            .ContainSingle(n => n.Type == NotificationType.AdoptionRequestCreated);
        ownerInbox[0].PetId.Should().Be(_petId);
        ownerInbox[0].IsRead.Should().BeFalse();
        adopterInbox.Should().BeEmpty();
    }

    [Fact]
    public async Task ApproveAdoptionRequest_NotifiesApprovedAdopterAndRejectedOthers()
    {
        var first = await CreateRequestAsync(_adopterClient);
        var second = await CreateRequestAsync(_otherAdopterClient);

        var approveResponse = await _ownerClient.PostAsync(
            TestConstants.ApiPaths.ApproveAdoptionRequest(first.Id),
            null
        );
        approveResponse.ShouldBeOk();

        var approvedInbox = await GetNotificationsAsync(_adopterClient);
        var rejectedInbox = await GetNotificationsAsync(_otherAdopterClient);
        var ownerInbox = await GetNotificationsAsync(_ownerClient);

        approvedInbox
            .Should()
            .ContainSingle(n => n.Type == NotificationType.AdoptionRequestApproved);
        approvedInbox[0].AdoptionRequestId.Should().Be(first.Id);
        rejectedInbox
            .Should()
            .ContainSingle(n => n.Type == NotificationType.AdoptionRequestRejected);
        rejectedInbox[0].AdoptionRequestId.Should().Be(second.Id);
        ownerInbox
            .Should()
            .OnlyContain(n => n.Type == NotificationType.AdoptionRequestCreated);
    }

    [Fact]
    public async Task RejectAdoptionRequest_NotifiesAdopter()
    {
        var created = await CreateRequestAsync(_adopterClient);

        var response = await _ownerClient.PatchAsJsonAsync(
            TestConstants.ApiPaths.AdoptionRequestStatus(created.Id),
            new UpdateAdoptionRequestStatusDto { Status = AdoptionStatus.Rejected }
        );
        response.ShouldBeOk();

        var adopterInbox = await GetNotificationsAsync(_adopterClient);
        adopterInbox
            .Should()
            .ContainSingle(n => n.Type == NotificationType.AdoptionRequestRejected);
        adopterInbox[0].AdoptionRequestId.Should().Be(created.Id);
    }

    [Fact]
    public async Task CancelAdoptionRequest_NotifiesOwner()
    {
        var created = await CreateRequestAsync(_adopterClient);

        var response = await _adopterClient.PostAsync(
            TestConstants.ApiPaths.CancelAdoptionRequest(created.Id),
            null
        );
        response.ShouldBeOk();

        var ownerInbox = await GetNotificationsAsync(_ownerClient);
        ownerInbox
            .Should()
            .Contain(n =>
                n.Type == NotificationType.AdoptionRequestCancelled && n.AdoptionRequestId == created.Id
            );
        (await GetNotificationsAsync(_adopterClient)).Should().BeEmpty();
    }

    [Fact]
    public async Task MarkPetAsAdopted_NotifiesPendingAdoptersOfRejection()
    {
        var created = await CreateRequestAsync(_adopterClient);

        var response = await _ownerClient.PostAsync(
            TestConstants.ApiPaths.MarkPetAsAdopted(_petId),
            null
        );
        response.ShouldBeOk();

        var adopterInbox = await GetNotificationsAsync(_adopterClient);
        adopterInbox
            .Should()
            .ContainSingle(n =>
                n.Type == NotificationType.AdoptionRequestRejected && n.AdoptionRequestId == created.Id
            );
    }

    [Fact]
    public async Task UnreadCount_AndMarkAsRead_UpdateInboxState()
    {
        await CreateRequestAsync(_adopterClient);

        var unreadResponse = await _ownerClient.GetAsync(
            TestConstants.ApiPaths.NotificationsUnreadCount
        );
        unreadResponse.ShouldBeOk();
        var unread = await unreadResponse.ReadApiResponseDataAsync<UnreadCountDto>();
        unread!.Count.Should().Be(1);

        var inbox = await GetNotificationsAsync(_ownerClient);
        var readResponse = await _ownerClient.PostAsync(
            TestConstants.ApiPaths.NotificationRead(inbox[0].Id),
            null
        );
        readResponse.ShouldBeOk();
        var read = await readResponse.ReadApiResponseDataAsync<NotificationResponseDto>();
        read!.IsRead.Should().BeTrue();

        var unreadAfter = await _ownerClient.GetAsync(
            TestConstants.ApiPaths.NotificationsUnreadCount
        );
        var unreadAfterData = await unreadAfter.ReadApiResponseDataAsync<UnreadCountDto>();
        unreadAfterData!.Count.Should().Be(0);
    }

    [Fact]
    public async Task MarkAsRead_ForAnotherUsersNotification_ReturnsNotFound()
    {
        await CreateRequestAsync(_adopterClient);
        var ownerInbox = await GetNotificationsAsync(_ownerClient);

        var response = await _adopterClient.PostAsync(
            TestConstants.ApiPaths.NotificationRead(ownerInbox[0].Id),
            null
        );

        response.ShouldBeNotFound();
    }

    [Fact]
    public async Task MarkAllAsRead_ClearsUnreadCount()
    {
        await CreateRequestAsync(_adopterClient);
        var otherPetResponse = await _ownerClient.PostAsJsonAsync(
            TestConstants.ApiPaths.Pets,
            TestConstants.DtoBuilders.CreateValidPetDto(name: "Luna")
        );
        var otherPet = await otherPetResponse.ReadApiResponseDataAsync<API.DTOs.Pet.PetResponseDto>();
        await _adopterClient.PostAsJsonAsync(
            TestConstants.ApiPaths.AdoptionRequests,
            new CreateAdoptionRequestDto
            {
                PetId = otherPet!.Id,
                Message = TestConstants.Chat.AdoptionRequestMessage,
            }
        );

        var response = await _ownerClient.PostAsync(
            TestConstants.ApiPaths.NotificationsReadAll,
            null
        );
        response.ShouldBeOk();
        var result = await response.ReadApiResponseDataAsync<MarkReadResultDto>();
        result!.MarkedCount.Should().Be(2);

        var unread = await _ownerClient.GetAsync(TestConstants.ApiPaths.NotificationsUnreadCount);
        var unreadData = await unread.ReadApiResponseDataAsync<UnreadCountDto>();
        unreadData!.Count.Should().Be(0);
    }

    private async Task<AdoptionRequestResponseDto> CreateRequestAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync(
            TestConstants.ApiPaths.AdoptionRequests,
            new CreateAdoptionRequestDto
            {
                PetId = _petId,
                Message = TestConstants.Chat.AdoptionRequestMessage,
            }
        );
        response.ShouldBeCreated();
        var created = await response.ReadApiResponseDataAsync<AdoptionRequestResponseDto>();
        return created!;
    }

    private static async Task<List<NotificationResponseDto>> GetNotificationsAsync(HttpClient client)
    {
        var response = await client.GetAsync(TestConstants.ApiPaths.Notifications);
        response.ShouldBeOk();
        var inbox = await response.ReadApiResponseDataAsync<List<NotificationResponseDto>>();
        return inbox ?? [];
    }
}
