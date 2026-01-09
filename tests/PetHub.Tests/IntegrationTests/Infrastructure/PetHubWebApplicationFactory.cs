using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Moq;
using PetHub.API.Data;
using PetHub.API.Services;

namespace PetHub.Tests.IntegrationTests.Infrastructure;

/// <summary>
/// Custom WebApplicationFactory for integration tests
/// </summary>
public class PetHubWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName;

    public Mock<ICloudinaryService>? MockCloudinaryService { get; set; }

    public PetHubWebApplicationFactory()
    {
        // Generate unique database name for test isolation
        _databaseName = $"PetHubTestDb_{Guid.NewGuid()}";
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Set JWT secret for testing
        Environment.SetEnvironmentVariable(
            "JWT_SECRET",
            "test_jwt_secret_key_for_integration_tests_1234567890_abcdef"
        );

        builder.ConfigureServices(services =>
        {
            // Remove the real database context
            var descriptor = services.SingleOrDefault(d =>
                d.ServiceType == typeof(DbContextOptions<AppDbContext>)
            );

            if (descriptor != null)
            {
                services.Remove(descriptor);
            }

            // Add InMemory database for testing
            // Note: InMemory does NOT support real transactions
            // Transactions in code will be ignored (with warnings)
            services.AddDbContext<AppDbContext>(options =>
            {
                options.UseInMemoryDatabase(_databaseName);
                // Suppress transaction warnings in tests
                options.ConfigureWarnings(w =>
                    w.Ignore(
                        Microsoft
                            .EntityFrameworkCore
                            .Diagnostics
                            .InMemoryEventId
                            .TransactionIgnoredWarning
                    )
                );
            });

            // Replace real Cloudinary service with mock if provided
            if (MockCloudinaryService != null)
            {
                var cloudinaryDescriptor = services.SingleOrDefault(d =>
                    d.ServiceType == typeof(ICloudinaryService)
                );

                if (cloudinaryDescriptor != null)
                {
                    services.Remove(cloudinaryDescriptor);
                }

                services.AddSingleton(MockCloudinaryService.Object);
            }
        });

        builder.UseEnvironment("Testing");
    }
}
