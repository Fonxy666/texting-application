using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;
using Moq;
using Server.Services.EmailSender;
using Xunit;

namespace Tests.Services;

public class EmailSenderTests
{
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly Mock<SmtpClient> _mockSmtpClient;
    private readonly EmailSender _emailSender;

    public EmailSenderTests()
    {
        _mockConfiguration = new Mock<IConfiguration>();
        _mockConfiguration.SetupGet(c => c["DeveloperEmail"]).Returns("developer@example.com");
        _mockConfiguration.SetupGet(c => c["DeveloperPassword"]).Returns("password");

        _mockSmtpClient = new Mock<SmtpClient>("smtp-mail.outlook.com", 587)
        {
            CallBase = true
        };

        _emailSender = new EmailSender(_mockConfiguration.Object)
        {
            SmtpClientFactory = () => _mockSmtpClient.Object
        };
    }

    [Fact]
    public async Task SendEmailAsync_ShouldSendEmail()
    {
        // Arrange
        var email = "user@example.com";
        var subject = "Test Subject";
        var message = "Test Message";

        _mockSmtpClient.Setup(client => client.SendMailAsync(It.IsAny<MailMessage>()))
            .Returns(Task.CompletedTask)
            .Verifiable();

        // Act
        await _emailSender.SendEmailAsync(email, subject, message);

        // Assert
        _mockSmtpClient.Verify(client => client.SendMailAsync(It.Is<MailMessage>(m =>
            m.From.Address == "developer@example.com" &&
            m.To[0].Address == email &&
            m.Subject == subject &&
            m.Body == message)), Times.Once);
    }

    [Fact]
    public async Task SendEmailWithLinkAsync_ShouldSendEmailWithLink()
    {
        // Arrange
        var email = "user@example.com";
        var subject = "Test Subject";
        var resetId = "reset-id";

        _mockSmtpClient.Setup(client => client.SendMailAsync(It.IsAny<MailMessage>()))
            .Returns(Task.CompletedTask)
            .Verifiable();

        var expectedLink = $"http://localhost:4200/password-reset/{WebUtility.UrlEncode(resetId)}/{email}";
        var expectedHtmlMessage = $"<br/><a href=\"{expectedLink}\">Reset Password</a>";

        // Act
        await _emailSender.SendEmailWithLinkAsync(email, subject, resetId);

        // Assert
        _mockSmtpClient.Verify(client => client.SendMailAsync(It.Is<MailMessage>(m =>
            m.From.Address == "developer@example.com" &&
            m.To[0].Address == email &&
            m.Subject == subject &&
            m.Body == expectedHtmlMessage &&
            m.IsBodyHtml)), Times.Once);
    }
}