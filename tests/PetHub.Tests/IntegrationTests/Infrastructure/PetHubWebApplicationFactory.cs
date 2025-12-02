using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PetHub.API.Data;

namespace PetHub.Tests.IntegrationTests.Infrastructure;

/// <summary>
/// Custom WebApplicationFactory for integration tests
/// Configures in-memory database and test environment
/// </summary>
public class PetHubWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName;

    public PetHubWebApplicationFactory()
    {
        // Generate a unique database name for each factory instance
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

            // Add in-memory database for testing
            // Use a unique database name to isolate tests
            services.AddDbContext<AppDbContext>(options =>
            {
                options.UseInMemoryDatabase(_databaseName);
            });
        });

        builder.UseEnvironment("Testing");
    }
}
