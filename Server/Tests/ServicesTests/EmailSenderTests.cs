using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;
using Moq;
using Server.Services.EmailSender;
using Xunit;
using Xunit.Abstractions;
using Assert = Xunit.Assert;

namespace Tests.ServicesTests;

public class EmailSenderTests
{
    private readonly ITestOutputHelper _testOutputHelper;
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly Mock<ISmtpClientWrapper> _mockSmtpClientWrapper;
    private readonly EmailSender _emailSender;

    public EmailSenderTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
        _mockConfiguration = new Mock<IConfiguration>();
        _mockSmtpClientWrapper = new Mock<ISmtpClientWrapper>();

        _mockConfiguration.Setup(c => c["DeveloperEmail"]).Returns("developer@example.com");
        _mockConfiguration.Setup(c => c["DeveloperPassword"]).Returns("password");

        _emailSender = new EmailSender(_mockConfiguration.Object)
        {
            SmtpClientFactory = () => _mockSmtpClientWrapper.Object
        };
    }

    [Fact]
    public async Task SendEmailAsync_ShouldSendEmail()
    {
        const string email = "recipient@example.com";
        const string subject = "Test Subject";
        const string message = "Test Message";

        _mockSmtpClientWrapper
            .Setup(c => c.SendMailAsync(It.IsAny<MailMessage>()))
            .Returns(Task.CompletedTask);

        await _emailSender.SendEmailAsync(email, subject, message);

        _mockSmtpClientWrapper.Verify(client => client.SendMailAsync(It.Is<MailMessage>(msg =>
            msg.From != null &&
            msg.From.Address == "developer@example.com" &&
            msg.To[0].Address == email &&
            msg.Subject == subject &&
            msg.Body == message
        )), Times.Once);

        _mockSmtpClientWrapper.VerifySet(client => client.Credentials = It.Is<NetworkCredential>(cred =>
            cred.UserName == "developer@example.com" &&
            cred.Password == "password"
        ));
    }

    [Fact]
    public async Task SendEmailWithLinkAsync_ShouldSendEmailWithLink()
    {
        const string email = "recipient@example.com";
        const string subject = "Password Reset";
        const string resetId = "reset-id";
        var expectedEncodedEmail = WebUtility.UrlEncode(email);
        var expectedLink = $"http://localhost:4200/password-reset/{resetId}/{expectedEncodedEmail}";
        var expectedHtmlMessage = $"<br/><a href=\"{expectedLink}\">Reset Password</a>";

        _mockSmtpClientWrapper
            .Setup(c => c.SendMailAsync(It.IsAny<MailMessage>()))
            .Returns(Task.CompletedTask);

        MailMessage sentMessage = null;
        _mockSmtpClientWrapper
            .Setup(c => c.SendMailAsync(It.IsAny<MailMessage>()))
            .Callback<MailMessage>(msg => sentMessage = msg)
            .Returns(Task.CompletedTask);

        await _emailSender.SendEmailWithLinkAsync(email, subject, resetId);

        Assert.NotNull(sentMessage);
        Assert.Equal("developer@example.com", sentMessage.From?.Address);
        Assert.Equal(email, sentMessage.To[0].Address);
        Assert.Equal(subject, sentMessage.Subject);
        Assert.True(sentMessage.IsBodyHtml);

        _mockSmtpClientWrapper.VerifySet(client => client.Credentials = It.Is<NetworkCredential>(cred =>
            cred.UserName == "developer@example.com" &&
            cred.Password == "password"
        ));
    }
}
