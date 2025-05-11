using System.Net;
using System.Net.Mail;
using Textinger.Shared.Responses;

namespace UserService.Services.EmailSender;

public class EmailSender(IConfiguration configuration, ILogger<EmailSender> logger) : IEmailSender
{
    private readonly string _developerMail = configuration["DeveloperEmail"]!;
    private readonly string _developerPw = configuration["DeveloperAppPassword"]!;

    private Func<ISmtpClientWrapper> SmtpClientFactory { get; set; } = () => new SmtpClientWrapper(new SmtpClient("smtp.gmail.com", 587)
    {
        EnableSsl = true
    });

    public async Task<ResponseBase> SendEmailAsync(string userEmail, string tokenType)
    {
        var message = tokenType switch
        {
            "registration" => $"Verification code: {EmailSenderCodeGenerator.GenerateLongToken(userEmail, "registration")}",
            "login" => $"Login code: {EmailSenderCodeGenerator.GenerateShortToken(userEmail, "login")}",
            _ => throw new ArgumentException("Invalid token type", nameof(tokenType))
        };

        try
        {
            var client = SmtpClientFactory();
            client.Credentials = new NetworkCredential(_developerMail, _developerPw);

            await client.SendMailAsync(new MailMessage(_developerMail, userEmail, "Verification code", message));
            return new Success();
        }
        catch (Exception e)
        {
            logger.LogError($"Failed to send email: {e.Message}");
            return new FailureWithMessage("Cannot send e-mail to this address.");
        }
    }

    public async Task<ResponseBase> SendEmailWithLinkAsync(string email, string subject, string resetId)
    {
        try
        {
            var client = SmtpClientFactory();
            client.Credentials = new NetworkCredential(_developerMail, _developerPw);

            var resetLink = $"http://localhost:4200/password-reset/{WebUtility.UrlEncode(resetId)}/{email}";
            var htmlMessage = $"<br/><a href=\"{resetLink}\">Reset Password</a>";

            await client.SendMailAsync(new MailMessage(_developerMail, email, subject, htmlMessage)
            {
                IsBodyHtml = true
            });
            return new Success();
        }
        catch (Exception e)
        {
            Console.WriteLine($"Failed to send email with link: {e.Message}");
            return new FailureWithMessage("Cannot send e-mail to this address.");
        }
    }
}