using System.Net;
using System.Net.Http.Headers;
using System.Text;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using PetHub.API.Data;
using PetHub.API.DTOs.PetImage;
using PetHub.API.Services;
using PetHub.Tests.Extensions;
using PetHub.Tests.IntegrationTests.Helpers;
using PetHub.Tests.IntegrationTests.Infrastructure;
using Xunit.Abstractions;

namespace PetHub.Tests.IntegrationTests.Controllers.PetsControllerTests;

/// <summary>
/// Integration tests for Pet Image upload and deletion endpoints
/// Tests the complete HTTP request/response flow including Cloudinary mock and database interactions
/// Uses MySQL via Testcontainers for realistic transaction testing
/// </summary>
public class PetImageTests : IDisposable
{
    private readonly PetHubWebApplicationFactory _factory;
    private HttpClient _client = null!;
    private Mock<ICloudinaryService> _mockCloudinaryService = null!;
    private string _authToken = string.Empty;
    private int _dogSpeciesId;
    private int _firstBreedId;
    private int _testPetId;
    private readonly ITestOutputHelper _output;

    public PetImageTests(ITestOutputHelper output)
    {
        _output = output;

        // Create a fresh factory per test to ensure mock registrations apply to the host
        _factory = new PetHubWebApplicationFactory();

        _mockCloudinaryService = new Mock<ICloudinaryService>();
        _factory.MockCloudinaryService = _mockCloudinaryService;

        // Create client AFTER setting mock
        _client = _factory.CreateClient();

        // Synchronously initialize async setup for each test
        ResetAndSeedDatabase().GetAwaiter().GetResult();
        var ids = LoadCommonIds().GetAwaiter().GetResult();
        (_dogSpeciesId, _firstBreedId) = ids;
        _authToken = AuthenticateTestUser().GetAwaiter().GetResult();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            _authToken
        );
        _testPetId = CreateTestPet().GetAwaiter().GetResult();

        _output.WriteLine($"Test setup completed. Pet ID: {_testPetId}");
    }

    private async Task ResetAndSeedDatabase()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await dbContext.Database.EnsureDeletedAsync();
        await dbContext.Database.EnsureCreatedAsync();
        await TestDataSeeder.SeedTestData(dbContext);
    }

    private async Task<(int dogSpeciesId, int firstBreedId)> LoadCommonIds()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var dogSpecies = await dbContext.Species.FirstOrDefaultAsync(s =>
            s.Name == TestConstants.SpeciesAndBreeds.DogName
        );
        var dogSpeciesId = dogSpecies?.Id ?? 0;

        var breeds = await dbContext.Breeds.Take(2).ToListAsync();
        var firstBreedId = breeds.Count > 0 ? breeds[0].Id : 0;

        return (dogSpeciesId, firstBreedId);
    }

    private async Task<string> AuthenticateTestUser()
    {
        var token = await AuthenticationHelper.LoginAndGetTokenAsync(
            _client,
            TestConstants.Users.Email,
            TestConstants.Passwords.ValidPassword
        );
        return token;
    }

    private async Task<int> CreateTestPet()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var authenticatedUser = await dbContext.Users.FirstAsync(u =>
            u.Email == TestConstants.Users.Email
        );

        var pet = new API.Models.Pet
        {
            Name = "Test Pet for Images",
            SpeciesId = _dogSpeciesId,
            BreedId = _firstBreedId,
            Gender = API.Enums.PetGender.Male,
            Size = API.Enums.PetSize.Medium,
            AgeInMonths = TestConstants.Pets.ValidAgeInMonths,
            Description = TestConstants.Pets.DefaultDescription,
            IsCastrated = true,
            IsVaccinated = true,
            UserId = authenticatedUser.Id,
        };

        dbContext.Pets.Add(pet);
        await dbContext.SaveChangesAsync();

        return pet.Id;
    }

    #region Upload Image Tests

    [Fact]
    public async Task UploadPetImage_WithValidImage_ReturnsOk()
    {
        // Arrange
        var uploadedUrl = TestConstants.ImageUrls.CloudinaryUploadedImage;
        _mockCloudinaryService
            .Setup(s => s.UploadImageAsync(It.IsAny<IFormFile>(), It.IsAny<string>()))
            .ReturnsAsync(uploadedUrl);

        var content = CreateMultipartFormDataContent(TestConstants.ImageUrls.TestImageFileName);

        // Debug: verify pet exists
        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var petExists = await dbContext.Pets.AnyAsync(p => p.Id == _testPetId);
            var petCount = await dbContext.Pets.CountAsync();
            _output.WriteLine(
                $"Pet exists: {petExists}, Total pets: {petCount}, Test Pet ID: {_testPetId}"
            );
        }

        // Act
        var response = await _client.PostAsync(
            TestConstants.ApiPaths.PetImages(_testPetId),
            content
        );

        // Debug: log response
        var responseBody = await response.Content.ReadAsStringAsync();
        _output.WriteLine($"Response Status: {response.StatusCode}");
        _output.WriteLine($"Response Body: {responseBody}");

        // Assert
        response.ShouldBeOk();
    }

    [Fact]
    public async Task UploadPetImage_WithValidImage_ReturnsUploadedImageData()
    {
        // Arrange
        var uploadedUrl = TestConstants.ImageUrls.CloudinaryUploadedImage;
        _mockCloudinaryService
            .Setup(s => s.UploadImageAsync(It.IsAny<IFormFile>(), It.IsAny<string>()))
            .ReturnsAsync(uploadedUrl);

        var content = CreateMultipartFormDataContent(TestConstants.ImageUrls.TestImageFileName);

        // Act
        var response = await _client.PostAsync(
            TestConstants.ApiPaths.PetImages(_testPetId),
            content
        );

        // Assert
        response.ShouldBeOk();

        var imageResponse = await response.ReadApiResponseDataAsync<PetImageResponseDto>();
        imageResponse.Should().NotBeNull();
        imageResponse!.Id.Should().BeGreaterThan(0);
        imageResponse.Url.Should().Be(uploadedUrl);
        imageResponse.PetId.Should().Be(_testPetId);
    }

    [Fact]
    public async Task UploadPetImage_SavesImageToDatabase()
    {
        // Arrange
        var uploadedUrl = TestConstants.ImageUrls.CloudinaryUploadedImage;
        _mockCloudinaryService
            .Setup(s => s.UploadImageAsync(It.IsAny<IFormFile>(), It.IsAny<string>()))
            .ReturnsAsync(uploadedUrl);

        var content = CreateMultipartFormDataContent(TestConstants.ImageUrls.TestImageFileName);

        // Act
        var response = await _client.PostAsync(
            TestConstants.ApiPaths.PetImages(_testPetId),
            content
        );

        // Assert
        response.ShouldBeOk();

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var savedImage = await dbContext.PetImages.FirstOrDefaultAsync(img =>
            img.PetId == _testPetId && img.Url == uploadedUrl
        );

        savedImage.Should().NotBeNull();
        savedImage!.Url.Should().Be(uploadedUrl);
        savedImage.PetId.Should().Be(_testPetId);
    }

    [Fact]
    public async Task UploadPetImage_CallsCloudinaryWithCorrectFolder()
    {
        // Arrange
        var uploadedUrl = TestConstants.ImageUrls.CloudinaryUploadedImage;
        _mockCloudinaryService
            .Setup(s => s.UploadImageAsync(It.IsAny<IFormFile>(), It.IsAny<string>()))
            .ReturnsAsync(uploadedUrl);

        var content = CreateMultipartFormDataContent(TestConstants.ImageUrls.TestImageFileName);

        // Act
        var response = await _client.PostAsync(
            TestConstants.ApiPaths.PetImages(_testPetId),
            content
        );

        // Assert
        response.ShouldBeOk();

        _mockCloudinaryService.Verify(
            s => s.UploadImageAsync(It.IsAny<IFormFile>(), $"pets/{_testPetId}"),
            Times.Once
        );
    }

    [Fact]
    public async Task UploadPetImage_WithoutAuthentication_ReturnsUnauthorized()
    {
        // Arrange
        _client.DefaultRequestHeaders.Authorization = null;
        var content = CreateMultipartFormDataContent(TestConstants.ImageUrls.TestImageFileName);

        // Act
        var response = await _client.PostAsync(
            TestConstants.ApiPaths.PetImages(_testPetId),
            content
        );

        // Assert
        response.ShouldBeUnauthorized();
    }

    [Fact]
    public async Task UploadPetImage_ForNonExistentPet_ReturnsNotFound()
    {
        // Arrange
        var uploadedUrl = TestConstants.ImageUrls.CloudinaryUploadedImage;
        _mockCloudinaryService
            .Setup(s => s.UploadImageAsync(It.IsAny<IFormFile>(), It.IsAny<string>()))
            .ReturnsAsync(uploadedUrl);

        var content = CreateMultipartFormDataContent(TestConstants.ImageUrls.TestImageFileName);

        // Act
        var response = await _client.PostAsync(
            TestConstants.ApiPaths.PetImages(TestConstants.NonExistentIds.Generic),
            content
        );

        // Assert
        response.ShouldBeNotFound();
    }

    [Fact]
    public async Task UploadPetImage_ForPetOwnedByAnotherUser_ReturnsForbidden()
    {
        // Arrange
        // Create a pet owned by another user
        int anotherUserPetId;
        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            // Create another user
            var anotherUser = new API.Models.User
            {
                Name = "Another User",
                Email = TestConstants.Users.AnotherEmail,
                PasswordHash = "hash",
                PhoneNumber = "11999999999",
                ZipCode = "01000000",
                State = "SP",
                City = "São Paulo",
                Neighborhood = "Centro",
                Street = "Rua Teste",
                StreetNumber = "456",
            };

            dbContext.Users.Add(anotherUser);
            await dbContext.SaveChangesAsync();

            var pet = new API.Models.Pet
            {
                Name = "Another Pet",
                SpeciesId = _dogSpeciesId,
                BreedId = _firstBreedId,
                Gender = API.Enums.PetGender.Male,
                Size = API.Enums.PetSize.Medium,
                AgeInMonths = 12,
                Description = "Pet owned by another user",
                IsCastrated = true,
                IsVaccinated = true,
                UserId = anotherUser.Id,
            };

            dbContext.Pets.Add(pet);
            await dbContext.SaveChangesAsync();
            anotherUserPetId = pet.Id;
        }

        var uploadedUrl = TestConstants.ImageUrls.CloudinaryUploadedImage;
        _mockCloudinaryService
            .Setup(s => s.UploadImageAsync(It.IsAny<IFormFile>(), It.IsAny<string>()))
            .ReturnsAsync(uploadedUrl);

        var content = CreateMultipartFormDataContent(TestConstants.ImageUrls.TestImageFileName);

        // Act
        var response = await _client.PostAsync(
            TestConstants.ApiPaths.PetImages(anotherUserPetId),
            content
        );

        // Assert
        var respBody = await response.Content.ReadAsStringAsync();
        _output.WriteLine($"Response Body: {respBody}");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task UploadPetImage_WithoutFile_ReturnsBadRequest()
    {
        // Arrange
        var content = new MultipartFormDataContent();

        // Act
        var response = await _client.PostAsync(
            TestConstants.ApiPaths.PetImages(_testPetId),
            content
        );

        // Assert
        response.ShouldBeBadRequest();
    }

    [Fact]
    public async Task UploadPetImage_WhenPetAlreadyHas5Images_ReturnsNotFound()
    {
        // Arrange
        // Add 5 images to the pet
        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            for (int i = 1; i <= 5; i++)
            {
                dbContext.PetImages.Add(
                    new API.Models.PetImage
                    {
                        PetId = _testPetId,
                        Url = $"https://example.com/image{i}.jpg",
                    }
                );
            }

            await dbContext.SaveChangesAsync();
        }

        var uploadedUrl = TestConstants.ImageUrls.CloudinaryUploadedImage;
        _mockCloudinaryService
            .Setup(s => s.UploadImageAsync(It.IsAny<IFormFile>(), It.IsAny<string>()))
            .ReturnsAsync(uploadedUrl);

        var content = CreateMultipartFormDataContent(TestConstants.ImageUrls.TestImageFileName);

        // Act
        var response = await _client.PostAsync(
            TestConstants.ApiPaths.PetImages(_testPetId),
            content
        );

        // Assert
        response.ShouldBeNotFound();
        var apiResponse = await response.ReadApiResponseAsync<object>();
        apiResponse.Should().NotBeNull();
        apiResponse!.Errors.Should().Contain(e => e.Contains("Maximum allowed is 5"));
    }

    [Fact]
    public async Task UploadPetImage_WhenCloudinaryFails_ReturnsInternalServerError()
    {
        // Arrange
        _mockCloudinaryService
            .Setup(s => s.UploadImageAsync(It.IsAny<IFormFile>(), It.IsAny<string>()))
            .ThrowsAsync(new Exception("Cloudinary upload failed"));

        var content = CreateMultipartFormDataContent(TestConstants.ImageUrls.TestImageFileName);

        // Act
        var response = await _client.PostAsync(
            TestConstants.ApiPaths.PetImages(_testPetId),
            content
        );

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
    }

    #endregion

    public void Dispose()
    {
        try
        {
            _factory.MockCloudinaryService = null;
            _factory.Dispose();
        }
        catch
        {
            // Best-effort cleanup; ignore disposal errors in test teardown
        }
    }

    #region Delete Image Tests

    [Fact]
    public async Task DeletePetImage_WithValidImage_ReturnsOk()
    {
        // Arrange
        var imageId = await CreatePetImage();

        _mockCloudinaryService
            .Setup(s => s.DeleteImageAsync(It.IsAny<string>()))
            .ReturnsAsync(true);

        // Act
        var response = await _client.DeleteAsync(
            TestConstants.ApiPaths.PetImageById(_testPetId, imageId)
        );

        // Assert
        response.ShouldBeOk();
    }

    [Fact]
    public async Task DeletePetImage_RemovesImageFromDatabase()
    {
        // Arrange
        var imageId = await CreatePetImage();

        _mockCloudinaryService
            .Setup(s => s.DeleteImageAsync(It.IsAny<string>()))
            .ReturnsAsync(true);

        // Act
        var response = await _client.DeleteAsync(
            TestConstants.ApiPaths.PetImageById(_testPetId, imageId)
        );

        // Assert
        response.ShouldBeOk();

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var deletedImage = await dbContext.PetImages.FindAsync(imageId);
        deletedImage.Should().BeNull();
    }

    [Fact]
    public async Task DeletePetImage_CallsCloudinaryDeleteWithCorrectPublicId()
    {
        // Arrange
        var imageUrl =
            "https://res.cloudinary.com/demo/image/upload/v1234567890/pets/123/image.jpg";
        var imageId = await CreatePetImage(imageUrl);

        _mockCloudinaryService
            .Setup(s => s.DeleteImageAsync(It.IsAny<string>()))
            .ReturnsAsync(true);

        // Act
        var response = await _client.DeleteAsync(
            TestConstants.ApiPaths.PetImageById(_testPetId, imageId)
        );

        // Assert
        response.ShouldBeOk();

        _mockCloudinaryService.Verify(s => s.DeleteImageAsync("pets/123/image"), Times.Once);
    }

    [Fact]
    public async Task DeletePetImage_WithoutAuthentication_ReturnsUnauthorized()
    {
        // Arrange
        var imageId = await CreatePetImage();
        _client.DefaultRequestHeaders.Authorization = null;

        // Act
        var response = await _client.DeleteAsync(
            TestConstants.ApiPaths.PetImageById(_testPetId, imageId)
        );

        // Assert
        response.ShouldBeUnauthorized();
    }

    [Fact]
    public async Task DeletePetImage_ForNonExistentPet_ReturnsNotFound()
    {
        // Arrange
        _mockCloudinaryService
            .Setup(s => s.DeleteImageAsync(It.IsAny<string>()))
            .ReturnsAsync(true);

        // Act
        var response = await _client.DeleteAsync(
            TestConstants.ApiPaths.PetImageById(
                TestConstants.NonExistentIds.Generic,
                TestConstants.NonExistentIds.Alternative1
            )
        );

        // Assert
        response.ShouldBeNotFound();
    }

    [Fact]
    public async Task DeletePetImage_ForNonExistentImage_ReturnsNotFound()
    {
        // Arrange
        _mockCloudinaryService
            .Setup(s => s.DeleteImageAsync(It.IsAny<string>()))
            .ReturnsAsync(true);

        // Act
        var response = await _client.DeleteAsync(
            TestConstants.ApiPaths.PetImageById(_testPetId, TestConstants.NonExistentIds.Generic)
        );

        // Assert
        response.ShouldBeNotFound();
    }

    [Fact]
    public async Task DeletePetImage_ForPetOwnedByAnotherUser_ReturnsForbidden()
    {
        // Arrange
        // Create a pet with an image owned by another user
        int anotherUserPetId;
        int imageId;
        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var anotherUser = new API.Models.User
            {
                Name = "Another User",
                Email = TestConstants.Users.AnotherEmail,
                PasswordHash = "hash",
                PhoneNumber = "11999999999",
                ZipCode = "01000000",
                State = "SP",
                City = "São Paulo",
                Neighborhood = "Centro",
                Street = "Rua Teste",
                StreetNumber = "789",
            };

            dbContext.Users.Add(anotherUser);
            await dbContext.SaveChangesAsync();

            var pet = new API.Models.Pet
            {
                Name = "Another Pet",
                SpeciesId = _dogSpeciesId,
                BreedId = _firstBreedId,
                Gender = API.Enums.PetGender.Male,
                Size = API.Enums.PetSize.Medium,
                AgeInMonths = 12,
                Description = "Pet owned by another user",
                IsCastrated = true,
                IsVaccinated = true,
                UserId = anotherUser.Id,
            };

            dbContext.Pets.Add(pet);
            await dbContext.SaveChangesAsync();
            anotherUserPetId = pet.Id;

            var image = new API.Models.PetImage
            {
                PetId = pet.Id,
                Url = "https://example.com/image.jpg",
            };

            dbContext.PetImages.Add(image);
            await dbContext.SaveChangesAsync();
            imageId = image.Id;
        }

        _mockCloudinaryService
            .Setup(s => s.DeleteImageAsync(It.IsAny<string>()))
            .ReturnsAsync(true);

        // Act
        var response = await _client.DeleteAsync(
            TestConstants.ApiPaths.PetImageById(anotherUserPetId, imageId)
        );

        // Assert
        var respBody2 = await response.Content.ReadAsStringAsync();
        _output.WriteLine($"Response Body: {respBody2}");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task DeletePetImage_WhenCloudinaryFails_ReturnsInternalServerError()
    {
        // Arrange
        var imageId = await CreatePetImage();

        _mockCloudinaryService
            .Setup(s => s.DeleteImageAsync(It.IsAny<string>()))
            .ThrowsAsync(new Exception("Cloudinary delete failed"));

        // Act
        var response = await _client.DeleteAsync(
            TestConstants.ApiPaths.PetImageById(_testPetId, imageId)
        );

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task GetPetImages_ReturnsAllImagesForPet()
    {
        // Arrange
        var imageId1 = await CreatePetImage(TestConstants.ImageUrls.Image1);
        var imageId2 = await CreatePetImage(TestConstants.ImageUrls.Image2);

        // Act
        var response = await _client.GetAsync(TestConstants.ApiPaths.PetImages(_testPetId));

        // Assert
        response.ShouldBeOk();

        var images = await response.ReadApiResponseDataAsync<List<PetImageResponseDto>>();
        images.Should().NotBeNull();
        images!.Should().HaveCount(2);
        images
            .Should()
            .Contain(img => img.Id == imageId1 && img.Url == TestConstants.ImageUrls.Image1);
        images
            .Should()
            .Contain(img => img.Id == imageId2 && img.Url == TestConstants.ImageUrls.Image2);
    }

    #endregion

    #region Helper Methods

    private MultipartFormDataContent CreateMultipartFormDataContent(string fileName)
    {
        var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes("fake image content"));
        fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("image/jpeg");
        content.Add(fileContent, "file", fileName);
        return content;
    }

    private async Task<int> CreatePetImage(string? url = null)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var image = new API.Models.PetImage
        {
            PetId = _testPetId,
            Url = url ?? TestConstants.ImageUrls.Default,
        };

        dbContext.PetImages.Add(image);
        await dbContext.SaveChangesAsync();

        return image.Id;
    }

    #endregion
}
