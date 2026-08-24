using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace PetHub.Tests.IntegrationTests.Infrastructure;

/// <summary>
/// Factory that enables the rate limiter with tight limits so tests can assert 429s.
/// Uses a non-Testing environment because Program.cs skips UseRateLimiter in Testing.
/// </summary>
public class RateLimitedWebApplicationFactory : PetHubWebApplicationFactory
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.UseEnvironment("RateLimitTest");
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        builder.UseEnvironment("RateLimitTest");
        builder.ConfigureHostConfiguration(config =>
        {
            config.AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["ASPNETCORE_ENVIRONMENT"] = "RateLimitTest",
                    ["RateLimiting:GlobalPermitLimit"] = "3",
                    ["RateLimiting:GlobalWindowSeconds"] = "60",
                    ["RateLimiting:AuthPermitLimit"] = "2",
                    ["RateLimiting:AuthWindowSeconds"] = "60",
                }
            );
        });

        return base.CreateHost(builder);
    }
}
