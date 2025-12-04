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
        public static readonly Guid ValidUserId = new("12345678-1234-1234-1234-123456789012");
        public static readonly Guid AnotherUserId = new("87654321-4321-4321-4321-210987654321");
        public static readonly Guid EmptyUserId = Guid.Empty;

        public const string ValidEmail = "test@example.com";
        public const string AnotherEmail = "user@example.com";
        public const string EmailWithDots = "user.name@domain.com";
        public const string EmailWithOrg = "admin@test.org";
        public const string EmptyEmail = "";

        public const string ValidPassword = "SecurePassword123!";
        public const string ValidUsername = "testuser";
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
    /// </summary>
    public static class Pets
    {
        public static readonly Guid ValidPetId = new("11111111-1111-1111-1111-111111111111");
        public static readonly Guid AnotherPetId = new("22222222-2222-2222-2222-222222222222");

        public const string ValidPetName = "Buddy";
        public const int ValidAgeInMonths = 24;
    }

    /// <summary>
    /// HTTP-related test data
    /// </summary>
    public static class Http
    {
        public const string ValidRequestPath = "/api/pets/999";
        public const string AnotherRequestPath = "/api/users/me";

        public const int StatusOk = 200;
        public const int StatusBadRequest = 400;
        public const int StatusNotFound = 404;
        public const int StatusForbidden = 403;
        public const int StatusConflict = 409;
        public const int StatusInternalServerError = 500;
        public const int StatusClientClosedRequest = 499;
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
        public const string DifferentPassword = "DifferentPassword456!";
        public const string WrongPassword = "WrongPassword456!";
        public const string WrongCasePassword = "securepassword123!";
        public const string ShortPassword = "short";
        public const string AveragePassword = "averageLengthPassword123";
        public const string LongPassword =
            "VeryLongPasswordWithLotsOfCharacters123456789!@#$%^&*()";
        public const string SpecialCharsPassword = "🔒🔑Password123!こんにちは";
        public const string MalformedHash = "not-a-valid-bcrypt-hash";
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
        public const int NonExistentSpeciesId = 999;

        // Species Names
        public const string DogName = "Dog";
        public const string CatName = "Cat";

        // Breed IDs
        public const int LabradorBreedId = 1;
        public const int NonExistentBreedId = 999;

        // Breed Names
        public const string LabradorName = "Labrador";
        public const string PoodleName = "Poodle";
        public const string SiameseName = "Siamês";
        public const string PersianName = "Persa";

        // Seeded Species Names (Portuguese)
        public const string DogNamePt = "Cachorro";
        public const string CatNamePt = "Gato";

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
        public const int NonExistentTagId1 = 999;
        public const int NonExistentTagId2 = 888;
        public const int NonExistentTagId3 = 777;

        // Tag Names
        public const string BlackTagName = "Black";
        public const string WhiteTagName = "White";
        public const string SpottedTagName = "Spotted";

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
                Category = PetHub.API.Enums.TagCategory.Color,
            };

        /// <summary>
        /// Creates a Spotted pattern tag instance for testing
        /// </summary>
        public static API.Models.Tag CreateSpottedTag() =>
            new()
            {
                Id = SpottedTagId,
                Name = SpottedTagName,
                Category = PetHub.API.Enums.TagCategory.Pattern,
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
    /// Integration test data - Common values for DTOs and requests
    /// </summary>
    public static class IntegrationTests
    {
        /// <summary>
        /// API Paths - Centralized endpoint paths for integration tests
        /// </summary>
        public static class ApiPaths
        {
            // Auth paths
            public const string AuthRegister = "/api/auth/register";
            public const string AuthLogin = "/api/auth/login";

            // Pet paths
            public const string Pets = "/api/pets";
            public const string PetsSearch = "/api/pets/search";
            public const string PetsMe = "/api/pets/me";

            public static string PetById(int petId) => $"/api/pets/{petId}";

            // User paths
            public const string UsersMe = "/api/users/me";

            // Adoption paths
            public const string AdoptionRequests = "/api/adoption/requests";

            public static string AdoptionRequestById(int requestId) =>
                $"/api/adoption/requests/{requestId}";

            public static string AdoptionRequestApprove(int requestId) =>
                $"/api/adoption/requests/{requestId}/approve";

            public static string AdoptionRequestReject(int requestId) =>
                $"/api/adoption/requests/{requestId}/reject";

            public static string PetAdoptionRequests(int petId) =>
                $"/api/adoption/pets/{petId}/requests";

            public static string MarkPetAsAdopted(int petId) =>
                $"/api/adoption/pets/{petId}/mark-adopted";
        }

        /// <summary>
        /// Pet names for integration tests
        /// These are the actual pet names seeded by TestDataSeeder in the test database
        /// </summary>
        public static class PetNames
        {
            public const string Rex = "Rex";
            public const string Luna = "Luna";
            public const string Max = "Max";
            public const string Mia = "Mia";
            public const string Bella = "Bella";
            public const string Thor = "Thor";
        }

        /// <summary>
        /// Tag names seeded by TestDataSeeder
        /// </summary>
        public static class SeededTags
        {
            public const string White = "Branco";
            public const string Black = "Preto";
            public const string Brown = "Marrom";
            public const string ShortCoat = "Curto";
            public const string LongCoat = "Longo";
        }

        /// <summary>
        /// User data seeded by TestDataSeeder
        /// </summary>
        public static class SeededUsers
        {
            public const string Name = "Test User";
            public const string Email = "test@pethub.com";
            public const string Password = "testpassword";
            public const string PhoneNumber = "11999999999";
            public const string ZipCode = "01000000";
            public const string State = "SP";
            public const string City = "São Paulo";
            public const string Neighborhood = "Centro";
            public const string Street = "Rua Teste";
            public const string StreetNumber = "123";
        }

        /// <summary>
        /// Common descriptions for integration tests
        /// </summary>
        public static class Descriptions
        {
            public const string Default = "A test pet for integration testing";
            public const string Updated = "Updated description";
            public const string Short = "Test description";
            public const string Detailed = "Testing all relationships";
        }

        /// <summary>
        /// Common image URLs for integration tests
        /// </summary>
        public static class ImageUrls
        {
            public const string Default = "https://example.com/pet.jpg";
            public const string Image1 = "https://example.com/pet1.jpg";
            public const string Image2 = "https://example.com/pet2.jpg";
            public const string Image3 = "https://example.com/pet3.jpg";
            public const string Updated = "https://example.com/updated.jpg";

            public static List<string> SingleImage() => new() { Default };

            public static List<string> MultipleImages() => new() { Image1, Image2, Image3 };
        }

        /// <summary>
        /// Common user data for integration tests
        /// </summary>
        public static class UserData
        {
            public const string DefaultName = "Test User";
            public const string UpdatedName = "Updated Name";
            public const string DefaultPhone = "11999887766";
            public const string UpdatedPhone = "11988776655";
            public const string DefaultPassword = "test123456";
            public const string UpdatedPassword = "newpassword123";
            public const string DefaultZipCode = "01310100";
            public const string DefaultState = "SP";
            public const string DefaultCity = "São Paulo";
            public const string DefaultNeighborhood = "Centro";
            public const string DefaultStreet = "Rua Test";
            public const string DefaultStreetNumber = "123";
        }

        /// <summary>
        /// Email patterns for integration tests
        /// </summary>
        public static class Emails
        {
            public const string Owner = "owner@example.com";
            public const string OtherUser = "other@example.com";
            public const string DeleteOwner = "deleteowner@example.com";
            public const string DeleteOther = "deleteother@example.com";
            public const string MarkOwner = "markowner@example.com";
            public const string MarkOther = "markother@example.com";
            public const string MyPetsUser = "mypetsuser@example.com";
            public const string MyPetsOther = "mypetsother@example.com";
            public const string Login = "login@example.com";
            public const string Nonexistent = "nonexistent@example.com";
            public const string TokenTest = "tokentest@example.com";
            public const string InvalidFormat = "invalid-email";
            public const string NewEmail = "newemail@example.com";
            public const string DefaultDomain = "@example.com";
            public const string DefaultAuthEmail = "test@example.com";
            public const string DefaultAuthPassword = "test123456";

            /// <summary>
            /// Generates a unique email for test isolation
            /// </summary>
            public static string GenerateUnique() => $"test-{Guid.NewGuid()}@example.com";
        }
    }

    /// <summary>
    /// DTO Builders for integration tests - Creates fully populated DTOs
    /// </summary>
    public static class DtoBuilders
    {
        /// <summary>
        /// Creates a valid CreatePetDto with default values
        /// </summary>
        public static API.DTOs.Pet.CreatePetDto CreateValidPetDto(
            int? speciesId = null,
            int? breedId = null,
            List<int>? tagIds = null,
            string? name = null,
            string? description = null
        ) =>
            new()
            {
                Name = name ?? IntegrationTests.PetNames.Rex,
                SpeciesId = speciesId ?? SpeciesAndBreeds.DogSpeciesId,
                BreedId = breedId ?? SpeciesAndBreeds.LabradorBreedId,
                Gender = PetHub.API.Enums.PetGender.Male,
                Size = PetHub.API.Enums.PetSize.Medium,
                AgeInMonths = 24,
                Description = description ?? IntegrationTests.Descriptions.Default,
                IsCastrated = true,
                IsVaccinated = true,
                ImageUrls = IntegrationTests.ImageUrls.SingleImage(),
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
                Description = description ?? IntegrationTests.Descriptions.Updated,
                BreedId = breedId,
                Gender = PetHub.API.Enums.PetGender.Female,
                Size = PetHub.API.Enums.PetSize.Large,
                AgeInMonths = 36,
                IsCastrated = true,
                IsVaccinated = true,
                ImageUrls = new List<string> { IntegrationTests.ImageUrls.Updated },
                TagIds = tagIds ?? new List<int>(),
            };

        /// <summary>
        /// Creates a valid CreateUserDto for registration
        /// </summary>
        public static API.DTOs.User.CreateUserDto CreateValidUserDto(
            string? email = null,
            string? name = null,
            string? password = null
        ) =>
            new()
            {
                Name = name ?? IntegrationTests.UserData.DefaultName,
                Email = email ?? $"test-{Guid.NewGuid()}@example.com",
                Password = password ?? IntegrationTests.UserData.DefaultPassword,
                PhoneNumber = IntegrationTests.UserData.DefaultPhone,
                ZipCode = IntegrationTests.UserData.DefaultZipCode,
                State = IntegrationTests.UserData.DefaultState,
                City = IntegrationTests.UserData.DefaultCity,
                Neighborhood = IntegrationTests.UserData.DefaultNeighborhood,
                Street = IntegrationTests.UserData.DefaultStreet,
                StreetNumber = IntegrationTests.UserData.DefaultStreetNumber,
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
