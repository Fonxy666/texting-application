namespace UserService.Services.EmailSender;

public interface IEmailSender
{
    Task<bool> SendEmailAsync(string email, string subject, string message);
    Task<bool> SendEmailWithLinkAsync(string email, string subject, string userId);
}