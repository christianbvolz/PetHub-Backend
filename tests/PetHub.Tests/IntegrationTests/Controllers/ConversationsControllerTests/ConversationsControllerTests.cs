using System.Net;
using FluentAssertions;
using PetHub.API.DTOs.AdoptionRequest;
using PetHub.API.DTOs.Chat;
using PetHub.API.DTOs.Pet;
using PetHub.API.DTOs.User;
using PetHub.Tests.Extensions;
using PetHub.Tests.IntegrationTests.Helpers;
using PetHub.Tests.IntegrationTests.Infrastructure;

namespace PetHub.Tests.IntegrationTests.Controllers.ConversationsControllerTests;

public class ConversationsControllerTests : IClassFixture<PetHubWebApplicationFactory>, IAsyncLifetime
{
    private readonly PetHubWebApplicationFactory _factory;
    private readonly HttpClient _ownerClient;
    private readonly HttpClient _adopterClient;
    private readonly HttpClient _strangerClient;
    private string _ownerToken = string.Empty;
    private string _adopterToken = string.Empty;
    private int _petId;
    private Guid _ownerId;
    private Guid _adopterId;

    public ConversationsControllerTests(PetHubWebApplicationFactory factory)
    {
        _factory = factory;
        _ownerClient = factory.CreateClient();
        _adopterClient = factory.CreateClient();
        _strangerClient = factory.CreateClient();
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
            email: "owner-chat@test.com"
        );
        _adopterToken = await AuthenticationHelper.RegisterAndGetTokenAsync(
            _adopterClient,
            email: "adopter-chat@test.com"
        );
        var strangerToken = await AuthenticationHelper.RegisterAndGetTokenAsync(
            _strangerClient,
            email: "stranger-chat@test.com"
        );

        _ownerClient.AddAuthToken(_ownerToken);
        _adopterClient.AddAuthToken(_adopterToken);
        _strangerClient.AddAuthToken(strangerToken);

        var petResponse = await _ownerClient.PostAsJsonAsync(
            TestConstants.ApiPaths.Pets,
            TestConstants.DtoBuilders.CreateValidPetDto()
        );
        petResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var pet = await petResponse.ReadApiResponseDataAsync<PetResponseDto>();
        _petId = pet!.Id;

        _ownerId = (await GetCurrentUser(_ownerClient)).Id;
        _adopterId = (await GetCurrentUser(_adopterClient)).Id;
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task CreateConversation_AboutPet_ReturnsCreatedWithPublicOwnerProfile()
    {
        var response = await _adopterClient.PostAsJsonAsync(
            TestConstants.ApiPaths.Conversations,
            new CreateConversationDto { PetId = _petId }
        );

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await response.ReadApiResponseAsync<ConversationResponseDto>();
        result!.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.PetId.Should().Be(_petId);
        result.Data.OtherParticipant.Id.Should().Be(_ownerId);
        result.Data.OtherParticipant.GetType().GetProperty("Email").Should().BeNull();
        response.Headers.Location.Should().NotBeNull();
    }

    [Fact]
    public async Task CreateConversation_WhenAlreadyExists_ReturnsOkSameId()
    {
        var first = await _adopterClient.PostAsJsonAsync(
            TestConstants.ApiPaths.Conversations,
            new CreateConversationDto { PetId = _petId }
        );
        first.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await first.ReadApiResponseDataAsync<ConversationResponseDto>();

        var second = await _adopterClient.PostAsJsonAsync(
            TestConstants.ApiPaths.Conversations,
            new CreateConversationDto { PetId = _petId }
        );

        second.StatusCode.Should().Be(HttpStatusCode.OK);
        var existing = await second.ReadApiResponseDataAsync<ConversationResponseDto>();
        existing!.Id.Should().Be(created!.Id);
    }

    [Fact]
    public async Task CreateConversation_ForOwnPet_ReturnsBadRequest()
    {
        var response = await _ownerClient.PostAsJsonAsync(
            TestConstants.ApiPaths.Conversations,
            new CreateConversationDto { PetId = _petId }
        );

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateConversation_ForMissingPet_ReturnsNotFound()
    {
        var response = await _adopterClient.PostAsJsonAsync(
            TestConstants.ApiPaths.Conversations,
            new CreateConversationDto { PetId = TestConstants.NonExistentIds.Generic }
        );

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateConversation_WithoutPetOrRequest_ReturnsBadRequest()
    {
        var response = await _adopterClient.PostAsJsonAsync(
            TestConstants.ApiPaths.Conversations,
            new CreateConversationDto()
        );

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateConversation_WithoutAuthentication_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            TestConstants.ApiPaths.Conversations,
            new CreateConversationDto { PetId = _petId }
        );

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateConversation_FromAdoptionRequest_LinksRequest()
    {
        var petId = await CreateOwnedPet();
        var requestResponse = await _adopterClient.PostAsJsonAsync(
            TestConstants.ApiPaths.AdoptionRequests,
            new CreateAdoptionRequestDto
            {
                PetId = petId,
                Message = TestConstants.Chat.AdoptionRequestMessage,
            }
        );
        requestResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var request = await requestResponse.ReadApiResponseDataAsync<AdoptionRequestResponseDto>();

        var conversationResponse = await _ownerClient.PostAsJsonAsync(
            TestConstants.ApiPaths.Conversations,
            new CreateConversationDto { AdoptionRequestId = request!.Id }
        );

        conversationResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var conversation = await conversationResponse.ReadApiResponseDataAsync<ConversationResponseDto>();
        conversation!.AdoptionRequestId.Should().Be(request.Id);
        conversation.OtherParticipant.Id.Should().Be(_adopterId);
        conversation.PetId.Should().Be(petId);
    }

    [Fact]
    public async Task CreateAdoptionRequest_AutomaticallyCreatesConversation()
    {
        var petId = await CreateOwnedPet();
        var requestResponse = await _adopterClient.PostAsJsonAsync(
            TestConstants.ApiPaths.AdoptionRequests,
            new CreateAdoptionRequestDto
            {
                PetId = petId,
                Message = TestConstants.Chat.AdoptionRequestMessage,
            }
        );
        requestResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var inbox = await _adopterClient.GetAsync(TestConstants.ApiPaths.Conversations);
        inbox.StatusCode.Should().Be(HttpStatusCode.OK);
        var conversations = await inbox.ReadApiResponseDataAsync<List<ConversationResponseDto>>();
        conversations.Should().Contain(c => c.PetId == petId);
        var conversation = conversations!.Single(c => c.PetId == petId);
        conversation.LastMessage.Should().NotBeNull();
        conversation.LastMessage!.Content.Should().Be(TestConstants.Chat.AdoptionRequestMessage);
    }

    [Fact]
    public async Task GetInbox_DoesNotIncludeOtherUsersConversations()
    {
        await _adopterClient.PostAsJsonAsync(
            TestConstants.ApiPaths.Conversations,
            new CreateConversationDto { PetId = _petId }
        );

        var response = await _strangerClient.GetAsync(TestConstants.ApiPaths.Conversations);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var inbox = await response.ReadApiResponseDataAsync<List<ConversationResponseDto>>();
        inbox.Should().BeEmpty();
    }

    [Fact]
    public async Task SendAndListMessages_PersistsHistoryForBothParticipants()
    {
        var conversation = await CreatePetConversation();

        var send = await _adopterClient.PostAsJsonAsync(
            TestConstants.ApiPaths.ConversationMessages(conversation.Id),
            new SendMessageDto { Content = TestConstants.Chat.ValidMessage }
        );
        send.StatusCode.Should().Be(HttpStatusCode.OK);
        var saved = await send.ReadApiResponseDataAsync<ChatMessageResponseDto>();
        saved!.SenderId.Should().Be(_adopterId);
        saved.SenderName.Should().NotBeNullOrWhiteSpace();
        saved.Content.Should().Be(TestConstants.Chat.ValidMessage);

        var history = await _ownerClient.GetAsync(
            TestConstants.ApiPaths.ConversationMessages(conversation.Id)
        );
        history.StatusCode.Should().Be(HttpStatusCode.OK);
        var messages = await history.ReadApiResponseDataAsync<List<ChatMessageResponseDto>>();
        messages.Should().ContainSingle(m => m.Id == saved.Id && m.Content == saved.Content);
    }

    [Fact]
    public async Task GetMessages_WithPageSize_ReturnsLatestThenOlderWithCursor()
    {
        var conversation = await CreatePetConversation();
        await _adopterClient.PostAsJsonAsync(
            TestConstants.ApiPaths.ConversationMessages(conversation.Id),
            new SendMessageDto { Content = TestConstants.Chat.ValidMessage }
        );
        await _ownerClient.PostAsJsonAsync(
            TestConstants.ApiPaths.ConversationMessages(conversation.Id),
            new SendMessageDto { Content = TestConstants.Chat.OwnerReply }
        );
        await _adopterClient.PostAsJsonAsync(
            TestConstants.ApiPaths.ConversationMessages(conversation.Id),
            new SendMessageDto { Content = TestConstants.Chat.SecondMessage }
        );

        var latestResponse = await _adopterClient.GetAsync(
            TestConstants.ApiPaths.ConversationMessagesPaged(conversation.Id, pageSize: 2)
        );
        var latest = await latestResponse.ReadApiResponseDataAsync<List<ChatMessageResponseDto>>();
        latest.Should().HaveCount(2);
        latest!.Select(m => m.Content)
            .Should()
            .Equal(TestConstants.Chat.OwnerReply, TestConstants.Chat.SecondMessage);

        var olderResponse = await _adopterClient.GetAsync(
            TestConstants.ApiPaths.ConversationMessagesPaged(
                conversation.Id,
                pageSize: 2,
                beforeId: latest[0].Id
            )
        );
        var older = await olderResponse.ReadApiResponseDataAsync<List<ChatMessageResponseDto>>();
        older.Should().ContainSingle();
        older![0].Content.Should().Be(TestConstants.Chat.ValidMessage);
    }

    [Fact]
    public async Task MarkAsRead_ClearsUnreadCountForCurrentUser()
    {
        var conversation = await CreatePetConversation();
        await _adopterClient.PostAsJsonAsync(
            TestConstants.ApiPaths.ConversationMessages(conversation.Id),
            new SendMessageDto { Content = TestConstants.Chat.ValidMessage }
        );

        var before = await _ownerClient.GetAsync(
            TestConstants.ApiPaths.ConversationById(conversation.Id)
        );
        var beforeDto = await before.ReadApiResponseDataAsync<ConversationResponseDto>();
        beforeDto!.UnreadCount.Should().Be(1);

        var read = await _ownerClient.PostAsync(
            TestConstants.ApiPaths.ConversationRead(conversation.Id),
            null
        );
        read.StatusCode.Should().Be(HttpStatusCode.OK);
        var readResult = await read.ReadApiResponseDataAsync<MessagesReadDto>();
        readResult!.MarkedCount.Should().Be(1);

        var after = await _ownerClient.GetAsync(
            TestConstants.ApiPaths.ConversationById(conversation.Id)
        );
        var afterDto = await after.ReadApiResponseDataAsync<ConversationResponseDto>();
        afterDto!.UnreadCount.Should().Be(0);
    }

    [Fact]
    public async Task GetConversation_WhenStranger_ReturnsForbidden()
    {
        var conversation = await CreatePetConversation();

        var response = await _strangerClient.GetAsync(
            TestConstants.ApiPaths.ConversationById(conversation.Id)
        );

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetConversation_WhenMissing_ReturnsNotFound()
    {
        var response = await _adopterClient.GetAsync(
            TestConstants.ApiPaths.ConversationById(TestConstants.NonExistentIds.Generic)
        );

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task SendMessage_WhenStranger_ReturnsForbidden()
    {
        var conversation = await CreatePetConversation();

        var response = await _strangerClient.PostAsJsonAsync(
            TestConstants.ApiPaths.ConversationMessages(conversation.Id),
            new SendMessageDto { Content = TestConstants.Chat.ValidMessage }
        );

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task SendMessage_WhenEmpty_ReturnsBadRequest()
    {
        var conversation = await CreatePetConversation();

        var response = await _adopterClient.PostAsJsonAsync(
            TestConstants.ApiPaths.ConversationMessages(conversation.Id),
            new SendMessageDto { Content = "" }
        );

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private async Task<ConversationResponseDto> CreatePetConversation()
    {
        var petId = await CreateOwnedPet();
        var response = await _adopterClient.PostAsJsonAsync(
            TestConstants.ApiPaths.Conversations,
            new CreateConversationDto { PetId = petId }
        );
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var conversation = await response.ReadApiResponseDataAsync<ConversationResponseDto>();
        return conversation!;
    }

    private async Task<int> CreateOwnedPet()
    {
        var petResponse = await _ownerClient.PostAsJsonAsync(
            TestConstants.ApiPaths.Pets,
            TestConstants.DtoBuilders.CreateValidPetDto(name: $"Pet-{Guid.NewGuid():N}"[..20])
        );
        petResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var pet = await petResponse.ReadApiResponseDataAsync<PetResponseDto>();
        return pet!.Id;
    }

    private static async Task<UserResponseDto> GetCurrentUser(HttpClient client)
    {
        var response = await client.GetAsync(TestConstants.ApiPaths.UsersMe);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var user = await response.ReadApiResponseDataAsync<UserResponseDto>();
        return user!;
    }
}
