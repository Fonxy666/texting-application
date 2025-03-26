using System.Net;
using System.Net.Mail;

namespace UserService.Services.EmailSender;

public class SmtpClientWrapper(SmtpClient smtpClient) : ISmtpClientWrapper
{
    public Task SendMailAsync(MailMessage message)
    {
        return smtpClient.SendMailAsync(message);
    }

    public ICredentialsByHost Credentials
    {
        get => smtpClient.Credentials;
        set => smtpClient.Credentials = value;
    }
}