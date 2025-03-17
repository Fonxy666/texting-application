namespace AuthenticationService.Model;

public class PrivateKey(string endToEndEncryptedPrivateKey, string iv)
{
    public string EndToEndEncryptedPrivateKey { get; init; } = endToEndEncryptedPrivateKey;
    public string Iv { get; private set; } = iv;
}