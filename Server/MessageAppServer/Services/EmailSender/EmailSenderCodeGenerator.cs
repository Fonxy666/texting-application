namespace Server.Services.EmailSender;

public static class EmailSenderCodeGenerator
{
    private static Dictionary<string, string> _regVerificationCodes = new();
    private static Dictionary<string, string> _loginVerificationCodes = new();
    public static string GenerateTokenForRegistration(string email)
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

        StoreVerificationCode(email, new string(token), "registration");
        return new string(token);
    }
    
    public static string GenerateTokenForLogin(string email)
    {
        const string characters = "0123456789";

        var rnd = new Random();
        var token = new char[6];

        for (var i = 0; i < 6; i++)
        {
            token[i] = characters[rnd.Next(characters.Length)];
        }

        StoreVerificationCode(email, new string(token), "login");
        return new string(token);
    }
    
    private static void StoreVerificationCode(string email, string code, string type)
    {
        if (type == "registration")
        {
            _regVerificationCodes[email] = code;
        }
        else
        {
            _loginVerificationCodes[email] = code;
        }
    }

    public static bool ExamineIfTheCodeWasOk(string email, string verifyCode, string type)
    {
        if (type == "registration")
        {
            _regVerificationCodes.TryGetValue(email, out var value);

            return _regVerificationCodes.TryGetValue(email, out var code) && code == verifyCode;
        }
        else
        {
            _loginVerificationCodes.TryGetValue(email, out var value);

            return _loginVerificationCodes.TryGetValue(email, out var code) && code == verifyCode;
        }
    }

    public static void RemoveVerificationCode(string email, string type)
    {
        if (type == "registration")
        {
            if (_regVerificationCodes.ContainsKey(email))
            {
                _regVerificationCodes.Remove(email);
            }
        }
        else
        {
            if (_loginVerificationCodes.ContainsKey(email))
            {
                _loginVerificationCodes.Remove(email);
            }
        }
    }
}