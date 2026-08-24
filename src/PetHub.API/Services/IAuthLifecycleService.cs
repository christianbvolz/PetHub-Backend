using PetHub.API.Models;

namespace PetHub.API.Services;

public interface IAuthLifecycleService
{
    Task SendVerificationEmailAsync(User user, CancellationToken cancellationToken = default);

    Task RequestVerificationEmailAsync(string email, CancellationToken cancellationToken = default);

    Task VerifyEmailAsync(string token, CancellationToken cancellationToken = default);

    Task RequestPasswordResetAsync(string email, CancellationToken cancellationToken = default);

    Task ResetPasswordAsync(
        string token,
        string newPassword,
        CancellationToken cancellationToken = default
    );
}
