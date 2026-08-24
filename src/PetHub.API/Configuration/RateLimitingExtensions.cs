using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace PetHub.API.Configuration;

public static class RateLimitingExtensions
{
    public static IServiceCollection AddPetHubRateLimiting(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        var settings =
            configuration.GetSection(RateLimitingSettings.SectionName).Get<RateLimitingSettings>()
            ?? new RateLimitingSettings();

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.OnRejected = async (context, cancellationToken) =>
            {
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                {
                    context.HttpContext.Response.Headers.RetryAfter = (
                        (int)retryAfter.TotalSeconds
                    ).ToString();
                }

                context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;

                await context.HttpContext.Response.WriteAsJsonAsync(
                    new ProblemDetails
                    {
                        Status = StatusCodes.Status429TooManyRequests,
                        Title = "Too many requests",
                        Detail = "Rate limit exceeded. Please try again later.",
                        Type = "https://httpstatuses.com/429",
                        Instance = context.HttpContext.Request.Path,
                    },
                    cancellationToken
                );
            };

            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(
                httpContext =>
                {
                    if (httpContext.Request.Path.StartsWithSegments("/health"))
                    {
                        return RateLimitPartition.GetNoLimiter("health");
                    }

                    var partitionKey =
                        httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

                    return RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey,
                        _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = settings.GlobalPermitLimit,
                            Window = TimeSpan.FromSeconds(settings.GlobalWindowSeconds),
                            QueueLimit = 0,
                            AutoReplenishment = true,
                        }
                    );
                }
            );

            options.AddPolicy(
                RateLimitingSettings.AuthPolicy,
                httpContext =>
                {
                    var partitionKey =
                        httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

                    return RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey,
                        _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = settings.AuthPermitLimit,
                            Window = TimeSpan.FromSeconds(settings.AuthWindowSeconds),
                            QueueLimit = 0,
                            AutoReplenishment = true,
                        }
                    );
                }
            );
        });

        return services;
    }
}
