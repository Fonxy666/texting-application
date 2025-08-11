using System.Net;
using System.Net.Mail;

namespace UserService.Services.EmailSender;

public interface ISmtpClientWrapper
{
    Task SendMailAsync(MailMessage message);
    ICredentialsByHost Credentials { get; set; }
}