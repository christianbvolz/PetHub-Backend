using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using pethub.Data;

namespace PetHub.Tests.IntegrationTests;

/// <summary>
/// Custom WebApplicationFactory for integration tests
/// Configures in-memory database and test environment
/// </summary>
public class PetHubWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
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
            // Use root provider to share database across all tests in same fixture
            services.AddDbContext<AppDbContext>(options =>
            {
                options.UseInMemoryDatabase("PetHubTestDb");
            });
        });

        builder.UseEnvironment("Testing");
    }
}
