using System.Net;
using System.Net.Mail;

namespace Server.Services.EmailSender;

public class EmailSender(IConfiguration configuration) : IEmailSender
{
    public Func<ISmtpClientWrapper> SmtpClientFactory { get; set; } = () => new SmtpClientWrapper(new SmtpClient("smtp-mail.outlook.com", 587)
    {
        EnableSsl = true
    });

    public async Task SendEmailAsync(string email, string subject, string message)
    {
        var mail = configuration["DeveloperEmail"];
        var pw = configuration["DeveloperPassword"];

        var client = SmtpClientFactory();
        client.Credentials = new NetworkCredential(mail, pw);

        await client.SendMailAsync(new MailMessage(mail, email, subject, message));
    }

    public async Task SendEmailWithLinkAsync(string email, string subject, string resetId)
    {
        var mail = configuration["DeveloperEmail"];
        var pw = configuration["DeveloperPassword"];

        var client = SmtpClientFactory();
        client.Credentials = new NetworkCredential(mail, pw);

        var resetLink = $"http://localhost:4200/password-reset/{WebUtility.UrlEncode(resetId)}/{email}";
        var htmlMessage = $"<br/><a href=\"{resetLink}\">Reset Password</a>";

        await client.SendMailAsync(new MailMessage(mail, email, subject, htmlMessage)
        {
            IsBodyHtml = true
        });
    }
}