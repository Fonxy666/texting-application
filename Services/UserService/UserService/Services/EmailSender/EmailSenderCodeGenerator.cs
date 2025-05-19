using System.Net;
using UserService.Models;

namespace UserService.Services.EmailSender;

public static class EmailSenderCodeGenerator
{
    private static readonly Dictionary<string, (string Code, DateTime Timestamp)> RegVerificationCodes = new();
    private static readonly Dictionary<string, (string Code, DateTime Timestamp)> LoginVerificationCodes = new();
    private static readonly Dictionary<string, (string Code, DateTime Timestamp)> PasswordResetCodes = new();
    private const int CodeExpirationMinutes = 2;
    
    public static string GenerateLongToken(string email, EmailType type)
    {
        const string characters = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";

        var rnd = new Random();
        var token = new char[19];

        for (var i = 0; i < 19; i++)
        {
            if (i is > 0 and 4 or > 0 and 9 or > 0 and 14)
            {
                token[i] = '-';
            }
            else
            {
                token[i] = characters[rnd.Next(characters.Length)];
            }
        }

        StoreVerificationCode(email, new string(token), type);
        return new string(token);
    }
    
    public static string GenerateShortToken(string email, EmailType type)
    {
        const string characters = "0123456789";

        var rnd = new Random();
        var token = new char[6];

        for (var i = 0; i < 6; i++)
        {
            token[i] = characters[rnd.Next(characters.Length)];
        }

        StoreVerificationCode(email, new string(token), type);
        return new string(token);
    }

    public static void StorePasswordResetCode(string email, string token)
    {
        StoreVerificationCode(email, token, EmailType.PasswordReset);
    }
    
    private static void StoreVerificationCode(string email, string code, EmailType type)
    {
        var timestamp = DateTime.UtcNow;
        switch (type)
        {
            case EmailType.Registration:
                RegVerificationCodes[email] = (code, timestamp);
                break;
            case EmailType.Login:
                LoginVerificationCodes[email] = (code, timestamp);
                break;
            case EmailType.PasswordReset:
                PasswordResetCodes[email] = (code, timestamp);
                break;
        }
    }

    public static bool ExamineIfTheCodeWasOk(string email, string verifyCode, EmailType type)
    {
        var timestamp = DateTime.UtcNow;
        var verificationCodes = type switch
        {
            EmailType.Registration => RegVerificationCodes,
            EmailType.Login => LoginVerificationCodes,
            EmailType.PasswordReset => PasswordResetCodes
        };

        if (!verificationCodes.TryGetValue(email, out var value)) return false;
        var decodedCode = type == EmailType.PasswordReset ? WebUtility.UrlDecode(value.Code) : value.Code;

        if (decodedCode != verifyCode) return false;
        return (timestamp - value.Timestamp).TotalMinutes <= CodeExpirationMinutes;
    }

    public static void RemoveVerificationCode(string email, EmailType type)
    {
        switch (type)
        {
            case EmailType.Registration:
                RegVerificationCodes.Remove(email);
                break;
            case EmailType.Login:
                LoginVerificationCodes.Remove(email);
                break;
            case EmailType.PasswordReset:
                PasswordResetCodes.Remove(email);
                break;
        }
    }
}