using System.ComponentModel.DataAnnotations;

namespace PetHub.API.Configuration;

public class RefreshTokenSettings
{
    public const string SectionName = "RefreshToken";

    [Required(ErrorMessage = "RefreshToken:ExpiresAtDays is required.")]
    [Range(1, 365, ErrorMessage = "Refresh token lifetime (days) must be between 1 and 365.")]
    public int ExpiresAtDays { get; set; } // Default 14 days
}
