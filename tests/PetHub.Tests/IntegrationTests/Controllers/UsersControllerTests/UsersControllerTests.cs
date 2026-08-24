using System.Text.Json;
using FluentAssertions;
using PetHub.API.Data;
using PetHub.API.DTOs.User;
using PetHub.API.Enums;
using PetHub.Tests.Extensions;
using PetHub.Tests.IntegrationTests.Helpers;
using PetHub.Tests.IntegrationTests.Infrastructure;

namespace PetHub.Tests.IntegrationTests.Controllers.UsersControllerTests;

/// <summary>
/// Integration tests for Users endpoints
/// </summary>
public class UsersIntegrationTests : IClassFixture<PetHubWebApplicationFactory>, IAsyncLifetime
{
    private readonly HttpClient _client;
    private readonly PetHubWebApplicationFactory _factory;
    private string _authToken = string.Empty;

    public UsersIntegrationTests(PetHubWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    public async Task InitializeAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await dbContext.Database.EnsureDeletedAsync();
        await dbContext.Database.EnsureCreatedAsync();
        await TestDataSeeder.SeedTestData(dbContext);

        _authToken = await AuthenticationHelper.RegisterAndGetTokenAsync(
            _client,
            TestConstants.Users.Email
        );
        _client.AddAuthToken(_authToken);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task GetCurrentUser_WithValidToken_ReturnsUserProfile()
    {
        // Act
        var response = await _client.GetAsync(TestConstants.ApiPaths.UsersMe);

        // Assert
        response.ShouldBeOk();

        var apiResponse = await response.ReadApiResponseAsync<UserResponseDto>();
        apiResponse.Should().NotBeNull();
        apiResponse!.Success.Should().BeTrue();
        apiResponse.Data.Should().NotBeNull();
        apiResponse.Data!.Email.Should().Be(TestConstants.Users.Email);
        apiResponse.Data.Name.Should().Be(TestConstants.Users.Username);
        apiResponse.Data.AccountType.Should().Be(PetHub.API.Enums.UserType.Person);
        apiResponse.Data.Cnpj.Should().BeEmpty();
        apiResponse.Data.PhoneNumber.Should().Be(TestConstants.Users.PhoneNumber);
        apiResponse.Data.City.Should().Be(TestConstants.Users.City);
        apiResponse.Data.State.Should().Be(TestConstants.Users.State);
        apiResponse.Data.Neighborhood.Should().Be(TestConstants.Users.Neighborhood);
        apiResponse.Data.Street.Should().Be(TestConstants.Users.Street);
        apiResponse.Data.StreetNumber.Should().Be(TestConstants.Users.StreetNumber);
    }

    [Fact]
    public async Task GetCurrentUser_IncludesPrivateContactAndAddressFields()
    {
        var response = await _client.GetAsync(TestConstants.ApiPaths.UsersMe);

        response.ShouldBeOk();

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        var user = json.GetProperty("data");
        user.GetProperty("email").GetString().Should().Be(TestConstants.Users.Email);
        user.GetProperty("phoneNumber").GetString().Should().Be(TestConstants.Users.PhoneNumber);
        user.GetProperty("neighborhood").GetString().Should().Be(TestConstants.Users.Neighborhood);
        user.GetProperty("street").GetString().Should().Be(TestConstants.Users.Street);
        user.GetProperty("streetNumber").GetString().Should().Be(TestConstants.Users.StreetNumber);
        user.GetProperty("accountType").GetString().Should().Be("Person");
        user.GetProperty("cnpj").GetString().Should().BeEmpty();
    }

    [Fact]
    public async Task GetCurrentUser_WithoutToken_ReturnsUnauthorized()
    {
        // Arrange
        var clientWithoutAuth = _factory.CreateClient();

        // Act
        var response = await clientWithoutAuth.GetAsync(TestConstants.ApiPaths.UsersMe);

        // Assert
        response.ShouldBeUnauthorized();
    }

    [Fact]
    public async Task PatchCurrentUser_WithValidData_UpdatesUser()
    {
        // Arrange
        var patchDto = TestConstants.DtoBuilders.CreatePatchUserDto(
            name: TestConstants.Users.UpdatedName,
            phoneNumber: TestConstants.Users.UpdatedPhone
        );

        // Act
        var response = await _client.PatchAsJsonAsync(TestConstants.ApiPaths.UsersMe, patchDto);

        // Assert
        response.ShouldBeOk();

        var apiResponse = await response.ReadApiResponseAsync<object>();
        apiResponse.Should().NotBeNull();
        apiResponse!.Success.Should().BeTrue();
        apiResponse.Message.Should().Be("User updated successfully.");

        // Verify the update
        var getResponse = await _client.GetAsync(TestConstants.ApiPaths.UsersMe);
        var userResponse = await getResponse.ReadApiResponseAsync<UserResponseDto>();
        userResponse!.Data!.Name.Should().Be(TestConstants.Users.UpdatedName);
        userResponse.Data.PhoneNumber.Should().Be(TestConstants.Users.UpdatedPhone);
    }

    [Fact]
    public async Task PatchCurrentUser_UpdateEmail_RequiresReauth()
    {
        // Arrange
        var patchDto = TestConstants.DtoBuilders.CreatePatchUserDto(
            email: TestConstants.Users.AnotherEmail
        );

        // Act
        var response = await _client.PatchAsJsonAsync(TestConstants.ApiPaths.UsersMe, patchDto);

        // Assert
        response.ShouldBeOk();

        var apiResponse = await response.ReadApiResponseAsync<Dictionary<string, object>>();
        apiResponse.Should().NotBeNull();
        apiResponse!.Success.Should().BeTrue();
        apiResponse
            .Message.Should()
            .Be("User updated successfully. Please login again with your new credentials.");

        apiResponse.Data.Should().ContainKey("requiresReauth");
    }

    [Fact]
    public async Task PatchCurrentUser_UpdateEmail_ResetsVerificationAndSendsEmail()
    {
        var newEmail = TestConstants.Users.GenerateUniqueEmail();
        var patchDto = TestConstants.DtoBuilders.CreatePatchUserDto(email: newEmail);

        var response = await _client.PatchAsJsonAsync(TestConstants.ApiPaths.UsersMe, patchDto);
        response.ShouldBeOk();

        var me = await (await _client.GetAsync(TestConstants.ApiPaths.UsersMe)).ReadApiResponseDataAsync<
            UserResponseDto
        >();
        me!.Email.Should().Be(newEmail);
        me.EmailVerified.Should().BeFalse();

        var token = _factory.EmailSender.RequireTokenFor(newEmail);
        token.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task PatchCurrentUser_UpdatePassword_RequiresReauth()
    {
        // Arrange
        var patchDto = TestConstants.DtoBuilders.CreatePatchUserDto(
            password: TestConstants.Passwords.AnotherValidPassword
        );

        // Act
        var response = await _client.PatchAsJsonAsync(TestConstants.ApiPaths.UsersMe, patchDto);

        // Assert
        response.ShouldBeOk();

        var apiResponse = await response.ReadApiResponseAsync<object>();
        apiResponse.Should().NotBeNull();
        apiResponse!.Success.Should().BeTrue();
        apiResponse
            .Message.Should()
            .Be("User updated successfully. Please login again with your new credentials.");
    }

    [Fact]
    public async Task PatchCurrentUser_WithoutToken_ReturnsUnauthorized()
    {
        // Arrange
        var clientWithoutAuth = _factory.CreateClient();
        var patchDto = TestConstants.DtoBuilders.CreatePatchUserDto(
            name: TestConstants.Users.Username
        );

        // Act
        var response = await clientWithoutAuth.PatchAsJsonAsync(
            TestConstants.ApiPaths.UsersMe,
            patchDto
        );

        // Assert
        response.ShouldBeUnauthorized();
    }

    [Fact]
    public async Task PatchCurrentUser_WithInvalidEmail_ReturnsBadRequest()
    {
        // Arrange
        var patchDto = TestConstants.DtoBuilders.CreatePatchUserDto(
            email: TestConstants.Users.InvalidEmail
        );

        // Act
        var response = await _client.PatchAsJsonAsync(TestConstants.ApiPaths.UsersMe, patchDto);

        // Assert
        response.ShouldBeBadRequest();
    }

    [Fact]
    public async Task DeleteCurrentUser_WithValidToken_DeletesUserAndSubsequentGetReturnsNotFound()
    {
        // Act
        var response = await _client.DeleteAsync(TestConstants.ApiPaths.UsersMe);

        // Assert
        response.ShouldBeOk();

        var apiResponse = await response.ReadApiResponseAsync<object>();
        apiResponse.Should().NotBeNull();
        apiResponse!.Success.Should().BeTrue();
        apiResponse.Message.Should().Be("User deleted successfully.");

        // Subsequent request to get current user should return NotFound
        var getResponse = await _client.GetAsync(TestConstants.ApiPaths.UsersMe);
        getResponse.ShouldBeNotFound();
    }

    [Fact]
    public async Task GetUser_WithoutToken_ReturnsSanitizedPublicProfile()
    {
        var meResponse = await _client.GetAsync(TestConstants.ApiPaths.UsersMe);
        var me = await meResponse.ReadApiResponseDataAsync<UserResponseDto>();
        me.Should().NotBeNull();

        var anonymousClient = _factory.CreateClient();
        var response = await anonymousClient.GetAsync(TestConstants.ApiPaths.UserById(me!.Id));

        response.ShouldBeOk();

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        var user = json.GetProperty("data");
        OwnerPrivacyAssertions.ShouldExposeOnlyPublicOwnerFields(user);
        user.GetProperty("id").GetGuid().Should().Be(me!.Id);
        user.GetProperty("name").GetString().Should().Be(TestConstants.Users.Username);
        user.GetProperty("accountType").GetString().Should().Be("Person");
    }

    [Fact]
    public async Task GetUser_DoesNotExposePrivateContactOrAddress()
    {
        var meResponse = await _client.GetAsync(TestConstants.ApiPaths.UsersMe);
        var me = await meResponse.ReadApiResponseDataAsync<UserResponseDto>();
        me.Should().NotBeNull();

        var response = await _factory
            .CreateClient()
            .GetAsync(TestConstants.ApiPaths.UserById(me!.Id));

        response.ShouldBeOk();

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        OwnerPrivacyAssertions.ShouldExposeOnlyPublicOwnerFields(json.GetProperty("data"));
    }

    [Fact]
    public async Task GetUser_WithUnknownId_ReturnsNotFound()
    {
        var response = await _factory
            .CreateClient()
            .GetAsync(TestConstants.ApiPaths.UserById(Guid.NewGuid()));

        response.ShouldBeNotFound();
    }

    [Fact]
    public async Task GetUser_Shelter_IncludesAccountTypeCnpjAndDescription()
    {
        var shelterClient = _factory.CreateClient();
        var registerResponse = await shelterClient.PostAsJsonAsync(
            TestConstants.ApiPaths.AuthRegister,
            TestConstants.DtoBuilders.CreateValidShelterDto()
        );
        registerResponse.ShouldBeOk();

        var login = await registerResponse.ReadApiResponseDataAsync<LoginResponseDto>();
        login.Should().NotBeNull();
        shelterClient.AddAuthToken(login!.Token);

        var me = await (
            await shelterClient.GetAsync(TestConstants.ApiPaths.UsersMe)
        ).ReadApiResponseDataAsync<UserResponseDto>();
        me.Should().NotBeNull();

        var response = await _factory
            .CreateClient()
            .GetAsync(TestConstants.ApiPaths.UserById(me!.Id));

        response.ShouldBeOk();

        var profile = await response.ReadApiResponseDataAsync<PublicUserResponseDto>();
        profile.Should().NotBeNull();
        profile!.AccountType.Should().Be(UserType.Shelter);
        profile.Cnpj.Should().Be(TestConstants.Users.ValidCnpj);
        profile.Description.Should().Be(TestConstants.Users.ShelterDescription);
        profile.Name.Should().Be(TestConstants.Users.ShelterName);
        profile.GetType().GetProperty("Email").Should().BeNull();
        profile.GetType().GetProperty("PhoneNumber").Should().BeNull();
        profile.GetType().GetProperty("Street").Should().BeNull();
    }

    [Fact]
    public async Task PatchCurrentUser_ConvertToShelter_UpdatesPublicProfile()
    {
        var patchDto = TestConstants.DtoBuilders.CreatePatchUserDto(
            accountType: UserType.Shelter,
            cnpj: TestConstants.Users.ValidCnpj,
            description: TestConstants.Users.ShelterDescription
        );

        var response = await _client.PatchAsJsonAsync(TestConstants.ApiPaths.UsersMe, patchDto);

        response.ShouldBeOk();

        var getResponse = await _client.GetAsync(TestConstants.ApiPaths.UsersMe);
        var user = await getResponse.ReadApiResponseDataAsync<UserResponseDto>();
        user.Should().NotBeNull();
        user!.AccountType.Should().Be(UserType.Shelter);
        user.Cnpj.Should().Be(TestConstants.Users.ValidCnpj);
        user.Description.Should().Be(TestConstants.Users.ShelterDescription);
    }
}
