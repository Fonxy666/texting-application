using Textinger.Shared.Responses;
using UserService.Models;

namespace UserService.Services.EmailSender;

public interface IEmailSender
{
    Task<ResponseBase> SendEmailAsync(string userEmail, EmailType tokenType);
    Task<ResponseBase> SendEmailWithLinkAsync(string email, string subject, string token);
}