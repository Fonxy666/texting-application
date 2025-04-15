using UserService.Model.Requests;
using UserService.Model.Responses;

namespace UserService.Services.EmailSender;

public interface IEmailSender
{
    Task<ResponseBase> SendEmailAsync(string UserEmail, string tokenType);
    Task<bool> SendEmailWithLinkAsync(string email, string subject, string userId);
}