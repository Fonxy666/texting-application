using Microsoft.AspNetCore.Identity;
using Moq;
using Server.Model;
using Server.Services.EmailSender;
using Xunit;
using Assert = NUnit.Framework.Assert;

namespace Tests.ServicesTests;

public class EmailCodeGeneratorTests
{
    private readonly Mock<UserManager<ApplicationUser>> _mockUserManager = MockUserManager.Create();

    [Fact]
    public void GenerateLongToken_ShouldGenerateTokenOfCorrectFormat()
    {
        const string email = "test@example.com";
        const string type = "registration";

        var token = EmailSenderCodeGenerator.GenerateLongToken(email, type);

        Assert.That(token, Is.Not.Null);
        Assert.That(token, Has.Length.EqualTo(19));
        Assert.Multiple(() =>
        {
            Assert.That(token[4], Is.EqualTo('-'));
            Assert.That(token[9], Is.EqualTo('-'));
            Assert.That(token[14], Is.EqualTo('-'));
        });

        var result = EmailSenderCodeGenerator.ExamineIfTheCodeWasOk(email, token, type);
        
        Assert.That(result, Is.EqualTo(true));
        
        EmailSenderCodeGenerator.RemoveVerificationCode(email, type);
        
        var resultAfterDelete = EmailSenderCodeGenerator.ExamineIfTheCodeWasOk(email, token, type);
        
        Assert.That(resultAfterDelete, Is.EqualTo(false));
    }

    [Fact]
    public void GenerateShortToken_ShouldGenerateTokenOfCorrectLength()
    {
        const string email = "test@example.com";
        const string type = "login";

        var token = EmailSenderCodeGenerator.GenerateShortToken(email, type);

        Assert.That(token, Is.Not.Null);
        Assert.That(token, Has.Length.EqualTo(6));

        var result = EmailSenderCodeGenerator.ExamineIfTheCodeWasOk(email, token, type);
        
        Assert.That(result, Is.EqualTo(true));

        EmailSenderCodeGenerator.RemoveVerificationCode(email, type);
        
        var resultAfterDelete = EmailSenderCodeGenerator.ExamineIfTheCodeWasOk(email, token, type);
        
        Assert.That(resultAfterDelete, Is.EqualTo(false));
    }
    
    [Fact]
    public void GenerateShortToken_ForPasswordReset_ShouldGenerateTokenOfCorrectLength()
    {
        const string email = "test@example.com";
        const string type = "passwordReset";

        const string token = "asdTest";
        
        EmailSenderCodeGenerator.StorePasswordResetCode(email, token);

        var result = EmailSenderCodeGenerator.ExamineIfTheCodeWasOk(email, token, type);
        
        Assert.That(result, Is.EqualTo(true));

        EmailSenderCodeGenerator.RemoveVerificationCode(email, type);
        
        var resultAfterDelete = EmailSenderCodeGenerator.ExamineIfTheCodeWasOk(email, token, type);
        
        Assert.That(resultAfterDelete, Is.EqualTo(false));
    }
}
