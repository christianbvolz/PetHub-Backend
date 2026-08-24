using System.Collections.Concurrent;
using PetHub.API.Services;

namespace PetHub.Tests.IntegrationTests.Helpers;

public sealed class CapturingEmailSender : IEmailSender
{
    public ConcurrentQueue<CapturedEmail> Messages { get; } = new();

    public Task SendAsync(
        string to,
        string subject,
        string textBody,
        string htmlBody,
        CancellationToken cancellationToken = default
    )
    {
        Messages.Enqueue(new CapturedEmail(to, subject, textBody, htmlBody));
        return Task.CompletedTask;
    }

    public CapturedEmail? FindByRecipient(string email) =>
        Messages.LastOrDefault(m =>
            string.Equals(m.To, email, StringComparison.OrdinalIgnoreCase)
        );

    public string RequireTokenFor(string email)
    {
        var message =
            FindByRecipient(email)
            ?? throw new InvalidOperationException($"No email was captured for {email}.");

        return PetHub.API.Utils.AuthEmailTemplates.ExtractToken(message.TextBody)
            ?? throw new InvalidOperationException($"No token found in email to {email}.");
    }
}

public sealed record CapturedEmail(string To, string Subject, string TextBody, string HtmlBody);
