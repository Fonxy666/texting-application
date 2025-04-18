using System.Net;
using System.Net.Mail;
using UserService.Models.Responses;

namespace UserService.Services.EmailSender;

public class EmailSender(IConfiguration configuration, ILogger<EmailSender> logger) : IEmailSender
{
    private readonly string _developerMail = configuration["DeveloperEmail"]!;
    private readonly string _developerPw = configuration["DeveloperAppPassword"]!;
    public Func<ISmtpClientWrapper> SmtpClientFactory { get; set; } = () => new SmtpClientWrapper(new SmtpClient("smtp.gmail.com", 587)
    {
        EnableSsl = true
    });

    public async Task<ResponseBase> SendEmailAsync(string UserEmail, string tokenType)
    {
        var message = tokenType switch
        {
            "registration" => $"Verification code: {EmailSenderCodeGenerator.GenerateLongToken(UserEmail, "registration")}",
            "login" => $"Login code: {EmailSenderCodeGenerator.GenerateShortToken(UserEmail, "login")}",
            _ => throw new ArgumentException("Invalid token type", nameof(tokenType))
        };

        var mail = configuration["DeveloperEmail"];
        var pw = configuration["DeveloperAppPassword"];

        try
        {
            var client = SmtpClientFactory();
            client.Credentials = new NetworkCredential(_developerMail, _developerPw);

            await client.SendMailAsync(new MailMessage(_developerMail, UserEmail, "Verification code", message));
            return new AuthResponseSuccess();
        }
        catch (Exception e)
        {
            logger.LogError($"Failed to send email: {e.Message}");
            return new FailedResponse();
        }
    }

    public async Task<ResponseBase> SendEmailWithLinkAsync(string email, string subject, string resetId)
    {
        var mail = configuration["DeveloperEmail"];
        var pw = configuration["DeveloperAppPassword"];

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
            return new UserResponseSuccess();
        }
        catch (Exception e)
        {
            Console.WriteLine($"Failed to send email with link: {e.Message}");
            return new FailedResponse();
        }
    }
}