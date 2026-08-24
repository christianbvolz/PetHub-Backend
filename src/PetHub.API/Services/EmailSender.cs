using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;
using PetHub.API.Configuration;

namespace PetHub.API.Services;

public class EmailSender(
    IOptions<SmtpSettings> smtp,
    IHostEnvironment environment,
    ILogger<EmailSender> logger
) : IEmailSender
{
    private readonly SmtpSettings _smtp = smtp.Value;

    public async Task SendAsync(
        string to,
        string subject,
        string textBody,
        string htmlBody,
        CancellationToken cancellationToken = default
    )
    {
        if (!_smtp.IsConfigured)
        {
            if (environment.IsProduction())
            {
                logger.LogWarning(
                    "SMTP is not configured. Email to {To} with subject {Subject} was not sent.",
                    to,
                    subject
                );
                return;
            }

            logger.LogInformation(
                "SMTP is not configured. Would send email to {To} with subject {Subject}:{NewLine}{Body}",
                to,
                subject,
                Environment.NewLine,
                textBody
            );
            return;
        }

        using var client = new SmtpClient(_smtp.Host, _smtp.Port)
        {
            EnableSsl = _smtp.EnableSsl,
            DeliveryMethod = SmtpDeliveryMethod.Network,
        };

        if (!string.IsNullOrWhiteSpace(_smtp.User))
        {
            client.Credentials = new NetworkCredential(_smtp.User, _smtp.Password);
        }

        using var message = new MailMessage
        {
            From = new MailAddress(_smtp.FromEmail, _smtp.FromName),
            Subject = subject,
            Body = string.IsNullOrWhiteSpace(htmlBody) ? textBody : htmlBody,
            IsBodyHtml = !string.IsNullOrWhiteSpace(htmlBody),
        };
        message.To.Add(to);

        cancellationToken.ThrowIfCancellationRequested();
        await client.SendMailAsync(message, cancellationToken);
    }
}
