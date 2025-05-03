using Textinger.Shared.Responses;

namespace UserService.Services.EmailSender;

public interface IEmailSender
{
    Task<ResponseBase> SendEmailAsync(string UserEmail, string tokenType);
    Task<ResponseBase> SendEmailWithLinkAsync(string email, string subject, string userId);
}