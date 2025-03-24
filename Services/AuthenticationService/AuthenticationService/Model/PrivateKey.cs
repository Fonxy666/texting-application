namespace AuthenticationService.Model;

public class PrivateKey
{
    public string EndToEndEncryptedPrivateKey { get; set; }
    public string Iv { get; set; }

    public PrivateKey() { }

    public PrivateKey(string endToEndEncryptedPrivateKey, string iv)
    {
        EndToEndEncryptedPrivateKey = endToEndEncryptedPrivateKey;
        Iv = iv;
    }
}