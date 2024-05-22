using System.Net;
using System.Net.Mail;

namespace Server.Services.EmailSender;

public class EmailSender(IConfiguration configuration) : IEmailSender
{
    public Func<SmtpClient> SmtpClientFactory { get; set; } = () => new SmtpClient("smtp-mail.outlook.com", 587)
    {
        EnableSsl = true,
        Credentials = new NetworkCredential(configuration["DeveloperEmail"], configuration["DeveloperPassword"])
    };

    private IConfiguration Configuration { get; } = configuration;

    public async Task SendEmailAsync(string email, string subject, string message)
    {
        var mail = Configuration["DeveloperEmail"];
        var pw = Configuration["DeveloperPassword"];

        var client = SmtpClientFactory();
        client.Credentials = new NetworkCredential(mail, pw);

        await client.SendMailAsync(new MailMessage(from: mail!, to: email, subject, message));
    }
    
    public async Task SendEmailWithLinkAsync(string email, string subject, string resetId)
    {
        var mail = Configuration["DeveloperEmail"];
        var pw = Configuration["DeveloperPassword"];

        var client = SmtpClientFactory();
        client.Credentials = new NetworkCredential(mail, pw);

        var resetLink = $"http://localhost:4200/password-reset/{WebUtility.UrlEncode(resetId)}/{email}";
        var htmlMessage = $"<br/><a href=\"{resetLink}\">Reset Password</a>";

        await client.SendMailAsync(new MailMessage(from: mail!, to: email, subject, htmlMessage)
        {
            IsBodyHtml = true
        });
    }
}