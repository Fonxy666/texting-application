using Textinger.Shared.Responses;
using UserService.Models;
using UserService.Services.EmailSender;

namespace UserServiceTests;

public class FakeEmailSender : IEmailSender
{
    public Task<ResponseBase> SendEmailAsync(string userEmail, EmailType tokenType)
    {
        return Task.FromResult<ResponseBase>(
            new Success());
    }

    public Task<ResponseBase> SendEmailWithLinkAsync(string email, string subject, string token)
    {
        return Task.FromResult<ResponseBase>(
            new Success());
    }
}