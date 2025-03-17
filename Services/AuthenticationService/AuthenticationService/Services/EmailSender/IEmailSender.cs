namespace AuthenticationService.Services.EmailSender;

public interface IEmailSender
{
    Task SendEmailAsync(string email, string subject, string message);
    Task SendEmailWithLinkAsync(string email, string subject, string userId);
}