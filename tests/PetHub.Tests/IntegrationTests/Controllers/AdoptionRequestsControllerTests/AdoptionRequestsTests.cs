using System.Net;
using FluentAssertions;
using PetHub.API.DTOs.AdoptionRequest;
using PetHub.API.DTOs.Common;
using PetHub.API.Enums;
using PetHub.Tests.Extensions;
using PetHub.Tests.IntegrationTests.Helpers;
using PetHub.Tests.IntegrationTests.Infrastructure;

namespace PetHub.Tests.IntegrationTests.Controllers.AdoptionRequestsControllerTests;

public class AdoptionRequestsTests : IClassFixture<PetHubWebApplicationFactory>, IAsyncLifetime
{
    private readonly PetHubWebApplicationFactory _factory;
    private readonly HttpClient _ownerClient;
    private readonly HttpClient _adopterClient;
    private string _ownerToken = string.Empty;
    private string _adopterToken = string.Empty;
    private int _petId;

    public AdoptionRequestsTests(PetHubWebApplicationFactory factory)
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

        // Create owner and adopter users
        _ownerToken = await AuthenticationHelper.RegisterAndGetTokenAsync(_ownerClient);
        _adopterToken = await AuthenticationHelper.RegisterAndGetTokenAsync(
            _adopterClient,
            email: "adopter@test.com"
        );

        // Owner creates a pet
        _ownerClient.AddAuthToken(_ownerToken);
        var petDto = TestConstants.DtoBuilders.CreateValidPetDto();
        var petResponse = await _ownerClient.PostAsJsonAsync(TestConstants.ApiPaths.Pets, petDto);
        petResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var petResult = await petResponse.ReadApiResponseAsync<API.DTOs.Pet.PetResponseDto>();
        _petId = petResult!.Data!.Id;

        // Setup adopter client
        _adopterClient.AddAuthToken(_adopterToken);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task CreateAdoptionRequest_WithValidData_ReturnsCreated()
    {
        // Arrange
        var dto = new CreateAdoptionRequestDto
        {
            PetId = _petId,
            Message = "I would love to adopt this pet! I have a big backyard.",
        };

        // Act
        var response = await _adopterClient.PostAsJsonAsync(
            TestConstants.ApiPaths.AdoptionRequests,
            dto
        );

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var result = await response.ReadApiResponseAsync<AdoptionRequestResponseDto>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.PetId.Should().Be(_petId);
        result.Data.Message.Should().Be(dto.Message);
        result.Data.Status.Should().Be(AdoptionStatus.Pending);
        result.Data.Pet.Should().NotBeNull();
        result.Data.Adopter.Should().NotBeNull();
    }

    [Fact]
    public async Task CreateAdoptionRequest_ForNonExistentPet_ReturnsNotFound()
    {
        // Arrange
        var dto = new CreateAdoptionRequestDto
        {
            PetId = TestConstants.NonExistentIds.Generic,
            Message = "I would love to adopt this pet!",
        };

        // Act
        var response = await _adopterClient.PostAsJsonAsync(
            TestConstants.ApiPaths.AdoptionRequests,
            dto
        );

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateAdoptionRequest_ForOwnPet_ReturnsBadRequest()
    {
        // Arrange - owner tries to adopt their own pet
        var dto = new CreateAdoptionRequestDto
        {
            PetId = _petId,
            Message = "I want to adopt my own pet.",
        };

        // Act
        var response = await _ownerClient.PostAsJsonAsync(
            TestConstants.ApiPaths.AdoptionRequests,
            dto
        );

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateAdoptionRequest_WithDuplicatePendingRequest_ReturnsBadRequest()
    {
        // Arrange - create first request
        var dto = new CreateAdoptionRequestDto
        {
            PetId = _petId,
            Message = "I would love to adopt this pet!",
        };

        var firstResponse = await _adopterClient.PostAsJsonAsync(
            TestConstants.ApiPaths.AdoptionRequests,
            dto
        );
        firstResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        // Act - try to create second request
        var response = await _adopterClient.PostAsJsonAsync(
            TestConstants.ApiPaths.AdoptionRequests,
            dto
        );

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetAdoptionRequest_AsAdopter_ReturnsRequest()
    {
        // Arrange - create a request
        var dto = new CreateAdoptionRequestDto
        {
            PetId = _petId,
            Message = "I would love to adopt this pet!",
        };

        var createResponse = await _adopterClient.PostAsJsonAsync(
            TestConstants.ApiPaths.AdoptionRequests,
            dto
        );
        var created = await createResponse.ReadApiResponseAsync<AdoptionRequestResponseDto>();
        var requestId = created!.Data!.Id;

        // Act
        var response = await _adopterClient.GetAsync(
            TestConstants.ApiPaths.AdoptionRequestById(requestId)
        );

        // Assert
        response.ShouldBeOk();
        var result = await response.ReadApiResponseAsync<AdoptionRequestResponseDto>();
        result!.Data!.Id.Should().Be(requestId);
    }

    [Fact]
    public async Task GetAdoptionRequest_AsOwner_ReturnsRequest()
    {
        // Arrange - adopter creates a request
        var dto = new CreateAdoptionRequestDto
        {
            PetId = _petId,
            Message = "I would love to adopt this pet!",
        };

        var createResponse = await _adopterClient.PostAsJsonAsync(
            TestConstants.ApiPaths.AdoptionRequests,
            dto
        );
        var created = await createResponse.ReadApiResponseAsync<AdoptionRequestResponseDto>();
        var requestId = created!.Data!.Id;

        // Act - owner tries to view
        var response = await _ownerClient.GetAsync(
            TestConstants.ApiPaths.AdoptionRequestById(requestId)
        );

        // Assert
        response.ShouldBeOk();
    }

    [Fact]
    public async Task GetAdoptionRequest_AsUnrelatedUser_ReturnsForbidden()
    {
        // Arrange - adopter creates a request
        var dto = new CreateAdoptionRequestDto
        {
            PetId = _petId,
            Message = "I would love to adopt this pet!",
        };

        var createResponse = await _adopterClient.PostAsJsonAsync(
            TestConstants.ApiPaths.AdoptionRequests,
            dto
        );
        var created = await createResponse.ReadApiResponseAsync<AdoptionRequestResponseDto>();
        var requestId = created!.Data!.Id;

        // Arrange - create unrelated user
        var unrelatedClient = _factory.CreateClient();
        var unrelatedToken = await AuthenticationHelper.RegisterAndGetTokenAsync(
            unrelatedClient,
            email: "unrelated@test.com"
        );
        unrelatedClient.AddAuthToken(unrelatedToken);

        // Act
        var response = await unrelatedClient.GetAsync(
            TestConstants.ApiPaths.AdoptionRequestById(requestId)
        );

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetMyRequests_ReturnsOnlyAdopterRequests()
    {
        // Arrange - create request by adopter
        var dto1 = new CreateAdoptionRequestDto { PetId = _petId, Message = "First request" };
        await _adopterClient.PostAsJsonAsync(TestConstants.ApiPaths.AdoptionRequests, dto1);

        // Arrange - create another user and send a request for same pet
        var otherClient = _factory.CreateClient();
        var otherToken = await AuthenticationHelper.RegisterAndGetTokenAsync(
            otherClient,
            email: "other@test.com"
        );
        otherClient.AddAuthToken(otherToken);
        var dtoOther = new CreateAdoptionRequestDto
        {
            PetId = _petId,
            Message = "Other user's request",
        };
        await otherClient.PostAsJsonAsync(TestConstants.ApiPaths.AdoptionRequests, dtoOther);

        // Act
        var response = await _adopterClient.GetAsync(TestConstants.ApiPaths.AdoptionRequestsSent);

        // Assert
        response.ShouldBeOk();
        var result = await response.ReadApiResponseAsync<List<AdoptionRequestResponseDto>>();
        result!.Data.Should().NotBeNull();
        result.Data.Should().HaveCount(1);
        result.Data![0].Message.Should().Be(dto1.Message);
    }

    [Fact]
    public async Task GetReceivedRequests_ReturnsOnlyOwnerRequests()
    {
        // Arrange - adopter creates a request for owner's pet
        var dto = new CreateAdoptionRequestDto
        {
            PetId = _petId,
            Message = "I would love to adopt!",
        };

        await _adopterClient.PostAsJsonAsync(TestConstants.ApiPaths.AdoptionRequests, dto);

        // Arrange - create another owner and pet, and have the adopter request that pet too
        var otherOwnerClient = _factory.CreateClient();
        var otherOwnerToken = await AuthenticationHelper.RegisterAndGetTokenAsync(
            otherOwnerClient,
            email: "otherowner@test.com"
        );
        otherOwnerClient.AddAuthToken(otherOwnerToken);

        var otherPetDto = TestConstants.DtoBuilders.CreateValidPetDto();
        var otherPetResponse = await otherOwnerClient.PostAsJsonAsync(
            TestConstants.ApiPaths.Pets,
            otherPetDto
        );
        otherPetResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var otherPetResult =
            await otherPetResponse.ReadApiResponseAsync<API.DTOs.Pet.PetResponseDto>();
        var otherPetId = otherPetResult!.Data!.Id;

        // Adopter creates a request for the other owner's pet
        var dtoOther = new CreateAdoptionRequestDto
        {
            PetId = otherPetId,
            Message = "Request for other pet",
        };
        await _adopterClient.PostAsJsonAsync(TestConstants.ApiPaths.AdoptionRequests, dtoOther);

        // Act - owner gets received requests (should only include requests for their pet)
        var response = await _ownerClient.GetAsync(TestConstants.ApiPaths.AdoptionRequestsReceived);

        // Assert
        response.ShouldBeOk();
        var result = await response.ReadApiResponseAsync<List<AdoptionRequestResponseDto>>();
        result!.Data.Should().HaveCount(1);
        result.Data![0].PetId.Should().Be(_petId);
    }

    [Fact]
    public async Task GetPetRequests_AsOwner_ReturnsRequests()
    {
        // Arrange - adopter creates a request
        var dto = new CreateAdoptionRequestDto
        {
            PetId = _petId,
            Message = "I would love to adopt!",
        };

        await _adopterClient.PostAsJsonAsync(TestConstants.ApiPaths.AdoptionRequests, dto);

        // Act
        var response = await _ownerClient.GetAsync(
            TestConstants.ApiPaths.PetAdoptionRequests(_petId)
        );

        // Assert
        response.ShouldBeOk();
        var result = await response.ReadApiResponseAsync<List<AdoptionRequestResponseDto>>();
        result!.Data.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetPetRequests_AsNonOwner_ReturnsForbidden()
    {
        // Act - adopter tries to get requests for a pet they don't own
        var response = await _adopterClient.GetAsync(
            TestConstants.ApiPaths.PetAdoptionRequests(_petId)
        );

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task UpdateRequestStatus_AsOwner_UpdatesStatus()
    {
        // Arrange - adopter creates a request
        var dto = new CreateAdoptionRequestDto
        {
            PetId = _petId,
            Message = "I would love to adopt!",
        };

        var createResponse = await _adopterClient.PostAsJsonAsync(
            TestConstants.ApiPaths.AdoptionRequests,
            dto
        );
        var created = await createResponse.ReadApiResponseAsync<AdoptionRequestResponseDto>();
        var requestId = created!.Data!.Id;

        // Act - owner approves request
        var updateDto = new UpdateAdoptionRequestStatusDto { Status = AdoptionStatus.Approved };

        var response = await _ownerClient.PatchAsJsonAsync(
            TestConstants.ApiPaths.AdoptionRequestStatus(requestId),
            updateDto
        );

        // Assert
        response.ShouldBeOk();
        var result = await response.ReadApiResponseAsync<AdoptionRequestResponseDto>();
        result!.Data!.Status.Should().Be(AdoptionStatus.Approved);
        result.Data.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task UpdateRequestStatus_AsNonOwner_ReturnsNotFound()
    {
        // Arrange - adopter creates a request
        var dto = new CreateAdoptionRequestDto
        {
            PetId = _petId,
            Message = "I would love to adopt!",
        };

        var createResponse = await _adopterClient.PostAsJsonAsync(
            TestConstants.ApiPaths.AdoptionRequests,
            dto
        );
        var created = await createResponse.ReadApiResponseAsync<AdoptionRequestResponseDto>();
        var requestId = created!.Data!.Id;

        // Act - adopter tries to update status
        var updateDto = new UpdateAdoptionRequestStatusDto { Status = AdoptionStatus.Approved };

        var response = await _adopterClient.PatchAsJsonAsync(
            TestConstants.ApiPaths.AdoptionRequestStatus(requestId),
            updateDto
        );

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateRequestStatus_ToRejected_UpdatesStatus()
    {
        // Arrange - adopter creates a request
        var dto = new CreateAdoptionRequestDto
        {
            PetId = _petId,
            Message = "I would love to adopt!",
        };

        var createResponse = await _adopterClient.PostAsJsonAsync(
            TestConstants.ApiPaths.AdoptionRequests,
            dto
        );
        var created = await createResponse.ReadApiResponseAsync<AdoptionRequestResponseDto>();
        var requestId = created!.Data!.Id;

        // Act - owner rejects request
        var updateDto = new UpdateAdoptionRequestStatusDto { Status = AdoptionStatus.Rejected };

        var response = await _ownerClient.PatchAsJsonAsync(
            TestConstants.ApiPaths.AdoptionRequestStatus(requestId),
            updateDto
        );

        // Assert
        response.ShouldBeOk();
        var result = await response.ReadApiResponseAsync<AdoptionRequestResponseDto>();
        result!.Data!.Status.Should().Be(AdoptionStatus.Rejected);
    }
}
