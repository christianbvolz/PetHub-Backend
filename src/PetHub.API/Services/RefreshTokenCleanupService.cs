using Microsoft.EntityFrameworkCore;
using PetHub.API.Data;

namespace PetHub.API.Services;

/// <summary>
/// Background service that periodically cleans up expired refresh tokens from the database.
/// Runs every hour to remove tokens that have expired.
/// </summary>
public class RefreshTokenCleanupService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<RefreshTokenCleanupService> _logger;
    private readonly TimeSpan _cleanupInterval = TimeSpan.FromHours(1);

    public RefreshTokenCleanupService(
        IServiceProvider serviceProvider,
        ILogger<RefreshTokenCleanupService> logger
    )
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "Refresh Token Cleanup Service is starting. Will run every {Interval}.",
            _cleanupInterval
        );

        // Wait a bit before first run (avoid startup congestion)
        await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CleanupExpiredTokensAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while cleaning up expired refresh tokens.");
            }

            await Task.Delay(_cleanupInterval, stoppingToken);
        }

        _logger.LogInformation("Refresh Token Cleanup Service is stopping.");
    }

    private async Task CleanupExpiredTokensAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var now = DateTime.UtcNow;

        // Find all expired tokens
        var expiredTokens = await dbContext
            .RefreshTokens.Where(rt => rt.ExpiresAt <= now)
            .ToListAsync(cancellationToken);

        if (expiredTokens.Count > 0)
        {
            dbContext.RefreshTokens.RemoveRange(expiredTokens);
            await dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Cleaned up {Count} expired refresh tokens.",
                expiredTokens.Count
            );
        }
        else
        {
            _logger.LogDebug("No expired refresh tokens found during cleanup.");
        }
    }
}
