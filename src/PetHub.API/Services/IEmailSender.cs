namespace PetHub.API.Services;

public interface IEmailSender
{
    Task SendAsync(
        string to,
        string subject,
        string textBody,
        string htmlBody,
        CancellationToken cancellationToken = default
    );
}
