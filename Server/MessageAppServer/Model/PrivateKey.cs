namespace AuthenticationServer.Model;

public class PrivateKey
{
    public Guid Id { get; init; }
    public Guid UserId { get; init; }
    public string EndToEndEncryptedPrivateKey { get; init; }
    public string Iv { get; set; }
    public PrivateKey() { }
    
    public PrivateKey(Guid userId, string endToEndEncryptedPrivateKey, string iv)
    {
        Id = new Guid();
        UserId = userId;
        EndToEndEncryptedPrivateKey = endToEndEncryptedPrivateKey;
        Iv = iv;
    }
}