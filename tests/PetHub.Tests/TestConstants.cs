using PetHub.API.Enums;

namespace PetHub.Tests;

/// <summary>
/// Centralized test constants and test data to avoid duplication and improve test readability
/// Shared across unit tests, integration tests, and test helpers
/// </summary>
public static class TestConstants
{
    /// <summary>
    /// User-related test data
    /// </summary>
    public static class Users
    {
        // User IDs
        public static readonly Guid ValidId = new("12345678-1234-1234-1234-123456789012");
        public static readonly Guid EmptyId = Guid.Empty;

        // Emails
        public const string Email = "test@example.com";
        public const string AnotherEmail = "user@example.com";
        public const string InvalidEmail = "invalid-email";

        /// <summary>
        /// Generates a unique email for test isolation
        /// </summary>
        public static string GenerateUniqueEmail() => $"test-{Guid.NewGuid()}@example.com";

        // User details
        public const string Username = "testuser";
        public const string PhoneNumber = "11999999999";
        public const string ZipCode = "01000000";
        public const string State = "SP";
        public const string City = "São Paulo";
        public const string Neighborhood = "Centro";
        public const string Street = "Rua Teste";
        public const string StreetNumber = "123";

        // Updated values for update tests
        public const string UpdatedName = "Updated Name";
        public const string UpdatedPhone = "11988776655";
    }

    /// <summary>
    /// JWT-related test data
    /// </summary>
    public static class Jwt
    {
        public const string SecretKey = "ThisIsAVerySecureSecretKeyForTestingPurposesOnly123456";
        public const string WrongSecretKey = "WrongSecretKeyThatShouldFailValidation12345678";
        public const string Issuer = "PetHubTestIssuer";
        public const string Audience = "PetHubTestAudience";
        public const int ExpirationMinutes = 60;
    }

    /// <summary>
    /// Pet-related test data
    /// Consolidated pet constants including names seeded by TestDataSeeder
    /// </summary>
    public static class Pets
    {
        // Pet Names (seeded by TestDataSeeder in integration tests)
        public const string Rex = "Rex";
        public const string Luna = "Luna";
        public const string Max = "Max";
        public const string Mia = "Mia";
        public const string Bella = "Bella";
        public const string Thor = "Thor";

        // Additional pet data constants
        public const int ValidAgeInMonths = 24;

        // Pet descriptions
        public const string DefaultDescription = "A test pet for integration testing";
        public const string UpdatedDescription = "Updated description";
        public const string ShortDescription = "Test description";
        public const string DetailedDescription = "Testing all relationships";
    }

    /// <summary>
    /// Exception messages for testing
    /// </summary>
    public static class ExceptionMessages
    {
        public const string PetNotFound = "Pet not found";
        public const string PetWithIdNotFound = "Pet with ID 999 not found";
        public const string InvalidTagId = "Invalid tag ID";
        public const string InvalidTagIdFormat = "Invalid tag ID format";
        public const string UserNotAuthorized = "User not authorized";
        public const string PetAlreadyAdopted = "Pet already adopted";
        public const string DatabaseConnectionFailed = "Database connection failed";
        public const string TestException = "Test exception";
        public const string ArgumentNullPetDto = "Value cannot be null. (Parameter 'petDto')";
    }

    /// <summary>
    /// Password-related test data
    /// </summary>
    public static class Passwords
    {
        public const string ValidPassword = "SecurePassword123!";
        public const string AnotherValidPassword = "Password123!";
        public const string DefaultAuthPassword = "test123456";
    }

    /// <summary>
    /// Non-existent IDs for testing error scenarios
    /// Centralized constants for IDs that don't exist in the database
    /// </summary>
    public static class NonExistentIds
    {
        /// <summary>
        /// Generic non-existent ID that can be used for any entity type
        /// </summary>
        public const int Generic = 999;

        /// <summary>
        /// Alternative non-existent IDs for testing multiple invalid IDs
        /// </summary>
        public const int Alternative1 = 888;
        public const int Alternative2 = 777;
    }

    /// <summary>
    /// Species and Breeds test data with factory methods
    /// Consolidated species and breed constants for both unit and integration tests
    /// </summary>
    public static class SpeciesAndBreeds
    {
        // Species IDs
        public const int DogSpeciesId = 1;
        public const int CatSpeciesId = 2;

        // Species Names
        public const string DogName = "Dog";
        public const string CatName = "Cat";

        // Breed IDs
        public const int LabradorBreedId = 1;

        // Breed Names
        public const string LabradorName = "Labrador";
        public const string PoodleName = "Poodle";
        public const string SiameseName = "Siamese";
        public const string PersianName = "Persian";

        /// <summary>
        /// Creates a Dog species instance for testing
        /// </summary>
        public static API.Models.Species CreateDogSpecies() =>
            new() { Id = DogSpeciesId, Name = DogName };

        /// <summary>
        /// Creates a Cat species instance for testing
        /// </summary>
        public static API.Models.Species CreateCatSpecies() =>
            new() { Id = CatSpeciesId, Name = CatName };

        /// <summary>
        /// Creates a Labrador breed instance for testing
        /// </summary>
        public static API.Models.Breed CreateLabradorBreed() =>
            new()
            {
                Id = LabradorBreedId,
                Name = LabradorName,
                SpeciesId = DogSpeciesId,
            };
    }

    /// <summary>
    /// Tag test data with factory methods
    /// Consolidated tag constants for both unit and integration tests
    /// </summary>
    public static class Tags
    {
        // Tag IDs
        public const int BlackTagId = 1;
        public const int WhiteTagId = 2;
        public const int SpottedTagId = 3;

        // Tag Names (for unit tests)
        public const string BlackTagName = "Black";
        public const string WhiteTagName = "White";
        public const string SpottedTagName = "Spotted";

        // Tag Names (seeded by TestDataSeeder)
        public const string WhiteName = "White";
        public const string BlackName = "Black";
        public const string BrownName = "Brown";
        public const string ShortCoatName = "Short Coat";
        public const string LongCoatName = "Long Coat";

        /// <summary>
        /// Creates a Black color tag instance for testing
        /// </summary>
        public static API.Models.Tag CreateBlackTag() =>
            new()
            {
                Id = BlackTagId,
                Name = BlackTagName,
                Category = API.Enums.TagCategory.Color,
            };

        /// <summary>
        /// Creates a White color tag instance for testing
        /// </summary>
        public static API.Models.Tag CreateWhiteTag() =>
            new()
            {
                Id = WhiteTagId,
                Name = WhiteTagName,
                Category = API.Enums.TagCategory.Color,
            };

        /// <summary>
        /// Creates a Spotted pattern tag instance for testing
        /// </summary>
        public static API.Models.Tag CreateSpottedTag() =>
            new()
            {
                Id = SpottedTagId,
                Name = SpottedTagName,
                Category = API.Enums.TagCategory.Pattern,
            };

        /// <summary>
        /// Creates a list of all standard test tags
        /// </summary>
        public static List<API.Models.Tag> CreateAllTags() =>
            [CreateBlackTag(), CreateWhiteTag(), CreateSpottedTag()];
    }

    /// <summary>
    /// Exception titles for middleware tests
    /// </summary>
    public static class ExceptionTitles
    {
        public const string ResourceNotFound = "Resource not found";
        public const string InvalidArgument = "Invalid argument";
        public const string AccessDenied = "Access denied";
        public const string InvalidOperation = "Invalid operation";
        public const string AnErrorOccurred = "An error occurred";
    }

    /// <summary>
    /// API Paths - Centralized endpoint paths for tests
    /// </summary>
    public static class ApiPaths
    {
        // Auth paths
        public const string AuthRegister = "/api/auth/register";
        public const string AuthLogin = "/api/auth/login";
        public const string AuthRefresh = "/api/auth/refresh";
        public const string AuthRevoke = "/api/auth/revoke";

        // Pet paths
        public const string Pets = "/api/pets";
        public const string PetsSearch = "/api/pets/search";
        public const string PetsMe = "/api/pets/me";
        public const string PetsFavorites = "/api/pets/me/favorites";

        public static string PetById(int petId) => $"/api/pets/{petId}";

        public static string PetFavorite(int petId) => $"/api/pets/{petId}/favorite";

        // User paths
        public const string UsersMe = "/api/users/me";

        // Adoption Request paths
        public const string AdoptionRequests = "/api/adoption-requests";
        public const string AdoptionRequestsSent = "/api/adoption-requests/me/sent";
        public const string AdoptionRequestsReceived = "/api/adoption-requests/me/received";

        public static string AdoptionRequestById(int requestId) =>
            $"/api/adoption-requests/{requestId}";

        public static string AdoptionRequestStatus(int requestId) =>
            $"/api/adoption-requests/{requestId}/status";

        public static string PetAdoptionRequests(int petId) =>
            $"/api/adoption-requests/pet/{petId}";

        public static string PetAdoptionRequestsPending(int petId) =>
            $"/api/adoption-requests/pet/{petId}/pending";

        public static string ApproveAdoptionRequest(int requestId) =>
            $"/api/adoption-requests/{requestId}/approve";

        public static string MarkPetAsAdopted(int petId) =>
            $"/api/adoption-requests/pet/{petId}/mark-adopted";

        // Pet Image paths
        public static string PetImages(int petId) => $"/api/pets/{petId}/images";

        public static string PetImageById(int petId, int imageId) =>
            $"/api/pets/{petId}/images/{imageId}";
    }

    /// <summary>
    /// Common image URLs for tests
    /// </summary>
    public static class ImageUrls
    {
        public const string Default = "https://example.com/pet.jpg";
        public const string Image1 = "https://example.com/pet1.jpg";
        public const string Image2 = "https://example.com/pet2.jpg";
        public const string Image3 = "https://example.com/pet3.jpg";
        public const string Updated = "https://example.com/updated.jpg";

        // Cloudinary-specific URLs for testing
        public const string CloudinaryUploadedImage =
            "https://res.cloudinary.com/demo/image/upload/v1234567890/pets/1/uploaded-image.jpg";
        public const string TestImageFileName = "test-image.jpg";

        public static List<string> SingleImage() => new() { Default };

        public static List<string> MultipleImages() => new() { Image1, Image2, Image3 };
    }

    /// <summary>
    /// DTO Builders - Creates fully populated DTOs for tests
    /// </summary>
    public static class DtoBuilders
    {
        /// <summary>
        /// Creates a valid CreatePetDto with default values
        /// </summary>
        public static API.DTOs.Pet.CreatePetDto CreateValidPetDto(
            string? name = null,
            int? speciesId = null,
            int? breedId = null,
            string? description = null,
            int? ageInMonths = null,
            PetGender? gender = null,
            PetSize? size = null,
            List<int>? tagIds = null
        ) =>
            new()
            {
                Name = name ?? Pets.Rex,
                SpeciesId = speciesId ?? SpeciesAndBreeds.DogSpeciesId,
                BreedId = breedId ?? SpeciesAndBreeds.LabradorBreedId,
                Gender = gender ?? PetGender.Male,
                Size = size ?? PetSize.Medium,
                AgeInMonths = ageInMonths ?? Pets.ValidAgeInMonths,
                Description = description ?? Pets.DefaultDescription,
                IsCastrated = true,
                IsVaccinated = true,
                ImageUrls = ImageUrls.SingleImage(),
                TagIds = tagIds ?? new List<int>(),
            };

        /// <summary>
        /// Creates a valid UpdatePetDto with default values
        /// </summary>
        public static API.DTOs.Pet.UpdatePetDto CreateValidUpdatePetDto(
            string? name = null,
            string? description = null,
            int? breedId = null,
            List<int>? tagIds = null
        ) =>
            new()
            {
                Name = name ?? "Updated Pet Name",
                Description = description ?? Pets.UpdatedDescription,
                BreedId = breedId,
                Gender = API.Enums.PetGender.Female,
                Size = API.Enums.PetSize.Large,
                AgeInMonths = 36,
                IsCastrated = true,
                IsVaccinated = true,
                ImageUrls = new List<string> { ImageUrls.Updated },
                TagIds = tagIds ?? new List<int>(),
            };

        /// <summary>
        /// Creates a valid CreateUserDto for registration
        /// </summary>
        public static API.DTOs.User.CreateUserDto CreateValidUserDto(
            string? name = null,
            string? email = null,
            string? password = null,
            string? phoneNumber = null,
            string? zipCode = null,
            string? state = null,
            string? city = null,
            string? neighborhood = null,
            string? street = null,
            string? streetNumber = null
        ) =>
            new()
            {
                Name = name ?? Users.Username,
                Email = email ?? Users.GenerateUniqueEmail(),
                Password = password ?? Passwords.ValidPassword,
                PhoneNumber = phoneNumber ?? Users.PhoneNumber,
                ZipCode = zipCode ?? Users.ZipCode,
                State = state ?? Users.State,
                City = city ?? Users.City,
                Neighborhood = neighborhood ?? Users.Neighborhood,
                Street = street ?? Users.Street,
                StreetNumber = streetNumber ?? Users.StreetNumber,
            };

        /// <summary>
        /// Creates a valid PatchUserDto with specific fields
        /// </summary>
        public static API.DTOs.User.PatchUserDto CreatePatchUserDto(
            string? name = null,
            string? email = null,
            string? password = null,
            string? phoneNumber = null
        ) =>
            new()
            {
                Name = name,
                Email = email,
                Password = password,
                PhoneNumber = phoneNumber,
            };

        /// <summary>
        /// Creates a valid LoginDto
        /// </summary>
        public static API.DTOs.User.LoginDto CreateLoginDto(string email, string password) =>
            new() { Email = email, Password = password };
    }
}
