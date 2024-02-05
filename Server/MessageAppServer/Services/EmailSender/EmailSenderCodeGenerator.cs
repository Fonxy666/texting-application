using Microsoft.AspNetCore.Mvc;

namespace Server.Services.EmailSender;

public static class EmailSenderCodeGenerator
{
    private static Dictionary<string, string> _verificationCodes = new();
    public static string GenerateToken(string email)
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

        StoreVerificationCode(email, new string(token));
        return new string(token);
    }
    
    private static void StoreVerificationCode(string email, string code)
    {
        _verificationCodes[email] = code;
    }

    public static bool ExamineIfTheCodeWasOk(string email, string verifyCode)
    {
        _verificationCodes.TryGetValue(email, out var value);
        Console.WriteLine(value);

        return _verificationCodes.TryGetValue(email, out var code) && code == verifyCode;
    }

    public static void RemoveVerificationCode(string email)
    {
        if (_verificationCodes.ContainsKey(email))
        {
            _verificationCodes.Remove(email);
        }
    }
}