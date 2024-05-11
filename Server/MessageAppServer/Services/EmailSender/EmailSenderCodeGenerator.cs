namespace Server.Services.EmailSender;

public static class EmailSenderCodeGenerator
{
    private static readonly Dictionary<string, string> RegVerificationCodes = new();
    private static readonly Dictionary<string, string> LoginVerificationCodes = new();
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
    
    private static void StoreVerificationCode(string email, string code, string type)
    {
        switch (type)
        {
            case "registration":
                RegVerificationCodes[email] = code;
                break;
            case "login":
                LoginVerificationCodes[email] = code;
                break;
        }
    }

    public static bool ExamineIfTheCodeWasOk(string email, string verifyCode, string type)
    {
        if (type == "registration")
        {
            RegVerificationCodes.TryGetValue(email, out var value);

            return RegVerificationCodes.TryGetValue(email, out var code) && code == verifyCode;
        }
        else
        {
            LoginVerificationCodes.TryGetValue(email, out var value);

            return LoginVerificationCodes.TryGetValue(email, out var code) && code == verifyCode;
        }
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
        else
        {
            if (LoginVerificationCodes.ContainsKey(email))
            {
                LoginVerificationCodes.Remove(email);
            }
        }
    }
}