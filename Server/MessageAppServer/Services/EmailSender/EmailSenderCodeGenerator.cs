using System.Net;

namespace Server.Services.EmailSender;

public static class EmailSenderCodeGenerator
{
    private static readonly Dictionary<string, (string Code, DateTime Timestamp)> RegVerificationCodes = new();
    private static readonly Dictionary<string, (string Code, DateTime Timestamp)> LoginVerificationCodes = new();
    private static readonly Dictionary<string, (string Code, DateTime Timestamp)> PasswordResetCodes = new();
    private const int CodeExpirationMinutes = 2;
    public static string GenerateLongToken(string email, string type)
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
    
    public static string GenerateShortToken(string email, string type)
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
        StoreVerificationCode(email, token, "passwordReset");
    }
    
    private static void StoreVerificationCode(string email, string code, string type)
    {
        var timestamp = DateTime.UtcNow;
        switch (type)
        {
            case "registration":
                RegVerificationCodes[email] = (code, timestamp);
                break;
            case "login":
                LoginVerificationCodes[email] = (code, timestamp);
                break;
            case "passwordReset":
                PasswordResetCodes[email] = (code, timestamp);
                break;
        }
    }

    public static bool ExamineIfTheCodeWasOk(string email, string verifyCode, string type)
    {
        var timestamp = DateTime.UtcNow;
        var verificationCodes = type switch
        {
            "registration" => RegVerificationCodes,
            "login" => LoginVerificationCodes,
            "passwordReset" => PasswordResetCodes
        };
        
        if (verificationCodes.TryGetValue(email, out var value))
        {
            var decodedCode = type == "passwordReset" ? WebUtility.UrlDecode(value.Code) : value.Code;

            if (decodedCode == verifyCode && (timestamp - value.Timestamp).TotalMinutes <= CodeExpirationMinutes)
            {
                RemoveVerificationCode(email, type);
                return true;
            }
            else
            {
                RemoveVerificationCode(email, type);
            }
        }

        return false;
    }

    public static void RemoveVerificationCode(string email, string type)
    {
        if (type == "registration")
        {
            if (RegVerificationCodes.ContainsKey(email))
            {
                RegVerificationCodes.Remove(email);
            }
        }
        else if (type == "login")
        {
            if (LoginVerificationCodes.ContainsKey(email))
            {
                LoginVerificationCodes.Remove(email);
            }
        }
        else if (type == "passwordReset")
        {
            if (PasswordResetCodes.ContainsKey(email))
            {
                PasswordResetCodes.Remove(email);
            }
        }
    }
}