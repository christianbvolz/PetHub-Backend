using System.ComponentModel.DataAnnotations;

namespace PetHub.API.Configuration;

public class RateLimitingSettings
{
    public const string SectionName = "RateLimiting";
    public const string AuthPolicy = "auth";

    [Range(1, 10000)]
    public int GlobalPermitLimit { get; set; } = 100;

    [Range(1, 3600)]
    public int GlobalWindowSeconds { get; set; } = 60;

    [Range(1, 1000)]
    public int AuthPermitLimit { get; set; } = 10;

    [Range(1, 3600)]
    public int AuthWindowSeconds { get; set; } = 60;
}
