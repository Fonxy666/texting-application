using UserService.Models.Responses;

namespace UserService.Services.EmailSender;

public interface IEmailSender
{
    Task<ResponseBase> SendEmailAsync(string UserEmail, string tokenType);
    Task<bool> SendEmailWithLinkAsync(string email, string subject, string userId);
}