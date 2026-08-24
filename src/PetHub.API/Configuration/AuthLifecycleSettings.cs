using System.ComponentModel.DataAnnotations;

namespace PetHub.API.Configuration;

public class AuthLifecycleSettings
{
    public const string SectionName = "AuthLifecycle";

    [Range(1, 168, ErrorMessage = "Email verification lifetime (hours) must be between 1 and 168.")]
    public int EmailVerificationExpiresHours { get; set; } = 24;

    [Range(1, 24, ErrorMessage = "Password reset lifetime (hours) must be between 1 and 24.")]
    public int PasswordResetExpiresHours { get; set; } = 1;

    [Required]
    [Url]
    public string FrontendBaseUrl { get; set; } = "http://localhost:5173";

    public string VerifyEmailPath { get; set; } = "/verify-email";

    public string ResetPasswordPath { get; set; } = "/reset-password";
}
