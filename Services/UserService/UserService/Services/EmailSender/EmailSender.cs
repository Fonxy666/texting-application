using System.Net;
using System.Net.Mail;

namespace UserService.Services.EmailSender;

public class EmailSender(IConfiguration configuration) : IEmailSender
{
    private readonly string _developerMail = configuration["DeveloperEmail"]!;
    private readonly string _developerPw = configuration["DeveloperAppPassword"]!;
    public Func<ISmtpClientWrapper> SmtpClientFactory { get; set; } = () => new SmtpClientWrapper(new SmtpClient("smtp.gmail.com", 587)
    {
        EnableSsl = true
    });

    public async Task<bool> SendEmailAsync(string email, string subject, string message)
    {
        var mail = configuration["DeveloperEmail"];
        var pw = configuration["DeveloperAppPassword"];

        try
        {
            var client = SmtpClientFactory();
            client.Credentials = new NetworkCredential(_developerMail, _developerPw);

            await client.SendMailAsync(new MailMessage(_developerMail, email, subject, message));
            return true;
        }
        catch (Exception e)
        {
            Console.WriteLine($"Failed to send email: {e.Message}");
            return false;
        }
    }

    public async Task<bool> SendEmailWithLinkAsync(string email, string subject, string resetId)
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
            return true;
        }
        catch (Exception e)
        {
            Console.WriteLine($"Failed to send email with link: {e.Message}");
            return false;
        }
    }
}