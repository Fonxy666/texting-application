namespace Server.Model;

public class PrivateKey
{
    public Guid Id { get; init; }
    public Guid UserId { get; init; }
    public string EndToEndEncryptedPrivateKey { get; init; }
    public PrivateKey() { }
    
    public PrivateKey(Guid userId, string endToEndEncryptedPrivateKey)
    {
        Id = new Guid();
        UserId = userId;
        EndToEndEncryptedPrivateKey = endToEndEncryptedPrivateKey;
    }
}