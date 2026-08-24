using System.Net;
using FluentAssertions;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using PetHub.API.DTOs.AdoptionRequest;
using PetHub.API.DTOs.Notification;
using PetHub.API.DTOs.Pet;
using PetHub.API.Enums;
using PetHub.API.Hubs;
using PetHub.Tests.Extensions;
using PetHub.Tests.IntegrationTests.Helpers;
using PetHub.Tests.IntegrationTests.Infrastructure;

namespace PetHub.Tests.IntegrationTests.Hubs;

public class NotificationHubTests : IClassFixture<PetHubWebApplicationFactory>, IAsyncLifetime
{
    private readonly PetHubWebApplicationFactory _factory;
    private readonly HttpClient _ownerClient;
    private readonly HttpClient _adopterClient;
    private string _ownerToken = string.Empty;
    private string _adopterToken = string.Empty;
    private int _petId;

    public NotificationHubTests(PetHubWebApplicationFactory factory)
    {
        _factory = factory;
        _ownerClient = factory.CreateClient();
        _adopterClient = factory.CreateClient();
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
            email: "owner-notify-hub@test.com"
        );
        _adopterToken = await AuthenticationHelper.RegisterAndGetTokenAsync(
            _adopterClient,
            email: "adopter-notify-hub@test.com"
        );

        _ownerClient.AddAuthToken(_ownerToken);
        _adopterClient.AddAuthToken(_adopterToken);

        var petResponse = await _ownerClient.PostAsJsonAsync(
            TestConstants.ApiPaths.Pets,
            TestConstants.DtoBuilders.CreateValidPetDto()
        );
        petResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var pet = await petResponse.ReadApiResponseDataAsync<PetResponseDto>();
        _petId = pet!.Id;
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
    public async Task CreateAdoptionRequest_PushesNotificationToOwnerHub()
    {
        await using var ownerConnection = CreateConnection(_ownerToken);
        var received = new TaskCompletionSource<NotificationResponseDto>(
            TaskCreationOptions.RunContinuationsAsynchronously
        );
        ownerConnection.On<NotificationResponseDto>(
            NotificationHub.ReceiveNotificationEvent,
            notification => received.TrySetResult(notification)
        );

        await ownerConnection.StartAsync();

        var createResponse = await _adopterClient.PostAsJsonAsync(
            TestConstants.ApiPaths.AdoptionRequests,
            new CreateAdoptionRequestDto
            {
                PetId = _petId,
                Message = TestConstants.Chat.AdoptionRequestMessage,
            }
        );
        createResponse.ShouldBeCreated();
        var created = await createResponse.ReadApiResponseDataAsync<AdoptionRequestResponseDto>();

        var payload = await received.Task.WaitAsync(TimeSpan.FromSeconds(5));
        payload.Type.Should().Be(NotificationType.AdoptionRequestCreated);
        payload.AdoptionRequestId.Should().Be(created!.Id);
        payload.PetId.Should().Be(_petId);
        payload.IsRead.Should().BeFalse();
    }

    private HubConnection CreateConnection(string? accessToken)
    {
        return new HubConnectionBuilder()
            .WithUrl(
                new Uri(_factory.Server.BaseAddress!, TestConstants.ApiPaths.NotificationHub),
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
