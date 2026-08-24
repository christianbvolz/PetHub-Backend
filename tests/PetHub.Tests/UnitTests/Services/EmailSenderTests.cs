using FluentAssertions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using PetHub.API.Configuration;
using PetHub.API.Services;

namespace PetHub.Tests.UnitTests.Services;

public class EmailSenderTests
{
    [Fact]
    public async Task SendAsync_WithoutSmtpInDevelopment_Completes()
    {
        var sender = CreateSender("Development");

        var act = async () =>
            await sender.SendAsync("user@example.com", "Subject", "text body", "<p>html</p>");

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task SendAsync_WithoutSmtpInProduction_CompletesWithoutSending()
    {
        var sender = CreateSender("Production");

        var act = async () =>
            await sender.SendAsync("user@example.com", "Subject", "text body", "<p>html</p>");

        await act.Should().NotThrowAsync();
    }

    private static EmailSender CreateSender(string environmentName)
    {
        var environment = new Mock<IHostEnvironment>();
        environment.Setup(e => e.EnvironmentName).Returns(environmentName);

        return new EmailSender(
            Options.Create(new SmtpSettings()),
            environment.Object,
            NullLogger<EmailSender>.Instance
        );
    }
}
