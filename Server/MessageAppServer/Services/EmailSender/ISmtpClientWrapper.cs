using System.Net;
using System.Net.Mail;

namespace AuthenticationServer.Services.EmailSender;

public interface ISmtpClientWrapper
{
    Task SendMailAsync(MailMessage message);
    ICredentialsByHost Credentials { get; set; }
}