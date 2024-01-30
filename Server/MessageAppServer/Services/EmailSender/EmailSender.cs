using System.Net;
using System.Net.Mail;

namespace Server.Services.EmailSender;

public class EmailSender(IConfiguration configuration) : IEmailSender
{
    public Task SendEmailAsync(string email, string subject, string message)
    {
        var mail = configuration["DeveloperEmail"];
        var pw = configuration["DeveloperPassword"];

        var client = new SmtpClient("smtp-mail.outlook.com", 587)
        {
            EnableSsl = true,
            Credentials = new NetworkCredential(mail, pw)
        };
        return client.SendMailAsync(new MailMessage(from: mail, to: email, subject, message));
    }
    
}