using System.ComponentModel.DataAnnotations;

namespace PetHub.API.Configuration;

/// <summary>
/// Strongly-typed configuration for JWT authentication
/// </summary>
public class JwtSettings
{
    public const string SectionName = "Jwt";

    [Required(ErrorMessage = "JWT SecretKey is required")]
    [MinLength(32, ErrorMessage = "JWT SecretKey must be at least 32 characters")]
    public required string SecretKey { get; set; }

    [Required(ErrorMessage = "JWT Issuer is required")]
    public required string Issuer { get; set; }

    [Required(ErrorMessage = "JWT Audience is required")]
    public required string Audience { get; set; }

    [Range(1, 60, ErrorMessage = "JWT ExpirationMinutes must be between 1 and 60.")]
    public int ExpirationMinutes { get; set; } = 60; // Default: 1 hour
}
