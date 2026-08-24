using System.Net;
using FluentAssertions;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using PetHub.API.DTOs.Chat;
using PetHub.API.DTOs.Pet;
using PetHub.API.Hubs;
using PetHub.Tests.IntegrationTests.Helpers;
using PetHub.Tests.IntegrationTests.Infrastructure;

namespace PetHub.Tests.IntegrationTests.Hubs;

public class ChatHubTests : IClassFixture<PetHubWebApplicationFactory>, IAsyncLifetime
{
    private readonly PetHubWebApplicationFactory _factory;
    private readonly HttpClient _ownerClient;
    private readonly HttpClient _adopterClient;
    private readonly HttpClient _strangerClient;
    private string _ownerToken = string.Empty;
    private string _adopterToken = string.Empty;
    private string _strangerToken = string.Empty;
    private int _conversationId;

    public ChatHubTests(PetHubWebApplicationFactory factory)
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
            email: "owner-hub@test.com"
        );
        _adopterToken = await AuthenticationHelper.RegisterAndGetTokenAsync(
            _adopterClient,
            email: "adopter-hub@test.com"
        );
        _strangerToken = await AuthenticationHelper.RegisterAndGetTokenAsync(
            _strangerClient,
            email: "stranger-hub@test.com"
        );

        _ownerClient.AddAuthToken(_ownerToken);
        _adopterClient.AddAuthToken(_adopterToken);
        _strangerClient.AddAuthToken(_strangerToken);

        var petResponse = await _ownerClient.PostAsJsonAsync(
            TestConstants.ApiPaths.Pets,
            TestConstants.DtoBuilders.CreateValidPetDto()
        );
        petResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var pet = await petResponse.ReadApiResponseDataAsync<PetResponseDto>();

        var conversationResponse = await _adopterClient.PostAsJsonAsync(
            TestConstants.ApiPaths.Conversations,
            new CreateConversationDto { PetId = pet!.Id }
        );
        conversationResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var conversation = await conversationResponse.ReadApiResponseDataAsync<ConversationResponseDto>();
        _conversationId = conversation!.Id;
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Connect_WithoutToken_ReturnsUnauthorized()
    {
        await using var connection = CreateConnection(accessToken: null);

        var act = async () => await connection.StartAsync();

        var exception = await act.Should().ThrowAsync<HttpRequestException>();
        exception.Which.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task SendMessage_PersistsAndBroadcastsServerSideSender()
    {
        await using var ownerConnection = CreateConnection(_ownerToken);
        await using var adopterConnection = CreateConnection(_adopterToken);

        var received = new TaskCompletionSource<ChatMessageResponseDto>(
            TaskCreationOptions.RunContinuationsAsynchronously
        );
        ownerConnection.On<ChatMessageResponseDto>(
            ChatHub.ReceiveMessageEvent,
            message => received.TrySetResult(message)
        );

        await ownerConnection.StartAsync();
        await adopterConnection.StartAsync();
        await ownerConnection.InvokeAsync(nameof(ChatHub.JoinChat), _conversationId);
        await adopterConnection.InvokeAsync(nameof(ChatHub.JoinChat), _conversationId);

        await adopterConnection.InvokeAsync(
            nameof(ChatHub.SendMessage),
            _conversationId,
            TestConstants.Chat.ValidMessage
        );

        var payload = await received.Task.WaitAsync(TimeSpan.FromSeconds(5));
        payload.Content.Should().Be(TestConstants.Chat.ValidMessage);
        payload.SenderName.Should().NotBeNullOrWhiteSpace();
        payload.ConversationId.Should().Be(_conversationId);

        var historyResponse = await _ownerClient.GetAsync(
            TestConstants.ApiPaths.ConversationMessages(_conversationId)
        );
        historyResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var history = await historyResponse.ReadApiResponseDataAsync<List<ChatMessageResponseDto>>();
        history.Should().ContainSingle(m => m.Id == payload.Id && m.Content == payload.Content);
    }

    [Fact]
    public async Task RestSendMessage_BroadcastsToConnectedHubClients()
    {
        await using var ownerConnection = CreateConnection(_ownerToken);
        var received = new TaskCompletionSource<ChatMessageResponseDto>(
            TaskCreationOptions.RunContinuationsAsynchronously
        );
        ownerConnection.On<ChatMessageResponseDto>(
            ChatHub.ReceiveMessageEvent,
            message => received.TrySetResult(message)
        );

        await ownerConnection.StartAsync();
        await ownerConnection.InvokeAsync(nameof(ChatHub.JoinChat), _conversationId);

        var send = await _adopterClient.PostAsJsonAsync(
            TestConstants.ApiPaths.ConversationMessages(_conversationId),
            new SendMessageDto { Content = TestConstants.Chat.SecondMessage }
        );
        send.StatusCode.Should().Be(HttpStatusCode.OK);

        var payload = await received.Task.WaitAsync(TimeSpan.FromSeconds(5));
        payload.Content.Should().Be(TestConstants.Chat.SecondMessage);
    }

    [Fact]
    public async Task JoinChat_WhenNotParticipant_ThrowsHubException()
    {
        await using var connection = CreateConnection(_strangerToken);
        await connection.StartAsync();

        var act = async () => await connection.InvokeAsync(nameof(ChatHub.JoinChat), _conversationId);

        await act.Should().ThrowAsync<HubException>().WithMessage("*permission*");
    }

    [Fact]
    public async Task JoinChat_WhenConversationMissing_ThrowsHubException()
    {
        await using var connection = CreateConnection(_adopterToken);
        await connection.StartAsync();

        var act = async () =>
            await connection.InvokeAsync(
                nameof(ChatHub.JoinChat),
                TestConstants.NonExistentIds.Generic
            );

        await act.Should().ThrowAsync<HubException>().WithMessage("*not found*");
    }

    [Fact]
    public async Task SendMessage_WhenNotParticipant_ThrowsHubException()
    {
        await using var connection = CreateConnection(_strangerToken);
        await connection.StartAsync();

        var act = async () =>
            await connection.InvokeAsync(
                nameof(ChatHub.SendMessage),
                _conversationId,
                TestConstants.Chat.ValidMessage
            );

        await act.Should().ThrowAsync<HubException>().WithMessage("*permission*");
    }

    [Fact]
    public async Task MarkAsRead_BroadcastsReadReceipt()
    {
        var conversationId = await CreateFreshConversation();

        await using var ownerConnection = CreateConnection(_ownerToken);
        await using var adopterConnection = CreateConnection(_adopterToken);

        var readReceipt = new TaskCompletionSource<MessagesReadDto>(
            TaskCreationOptions.RunContinuationsAsynchronously
        );
        adopterConnection.On<MessagesReadDto>(
            ChatHub.MessagesReadEvent,
            dto => readReceipt.TrySetResult(dto)
        );

        await ownerConnection.StartAsync();
        await adopterConnection.StartAsync();
        await ownerConnection.InvokeAsync(nameof(ChatHub.JoinChat), conversationId);
        await adopterConnection.InvokeAsync(nameof(ChatHub.JoinChat), conversationId);

        await adopterConnection.InvokeAsync(
            nameof(ChatHub.SendMessage),
            conversationId,
            TestConstants.Chat.ValidMessage
        );
        await ownerConnection.InvokeAsync(nameof(ChatHub.MarkAsRead), conversationId);

        var payload = await readReceipt.Task.WaitAsync(TimeSpan.FromSeconds(5));
        payload.ConversationId.Should().Be(conversationId);
        payload.MarkedCount.Should().Be(1);
    }

    private async Task<int> CreateFreshConversation()
    {
        var petResponse = await _ownerClient.PostAsJsonAsync(
            TestConstants.ApiPaths.Pets,
            TestConstants.DtoBuilders.CreateValidPetDto(name: $"Hub-{Guid.NewGuid():N}"[..20])
        );
        petResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var pet = await petResponse.ReadApiResponseDataAsync<PetResponseDto>();

        var conversationResponse = await _adopterClient.PostAsJsonAsync(
            TestConstants.ApiPaths.Conversations,
            new CreateConversationDto { PetId = pet!.Id }
        );
        conversationResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var conversation = await conversationResponse.ReadApiResponseDataAsync<ConversationResponseDto>();
        return conversation!.Id;
    }

    private HubConnection CreateConnection(string? accessToken)
    {
        return new HubConnectionBuilder()
            .WithUrl(
                new Uri(_factory.Server.BaseAddress!, TestConstants.ApiPaths.ChatHub),
                options =>
                {
                    options.HttpMessageHandlerFactory = _ => _factory.Server.CreateHandler();
                    options.Transports = HttpTransportType.LongPolling;
                    if (accessToken != null)
                    {
                        options.AccessTokenProvider = () => Task.FromResult<string?>(accessToken);
                    }
                }
            )
            .Build();
    }
}
