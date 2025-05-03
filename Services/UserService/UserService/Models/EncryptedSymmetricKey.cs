namespace UserService.Models;

public class EncryptedSymmetricKey
{
    public Guid KeyId { get; init; }
    public Guid UserId { get; init; }
    public ApplicationUser User { get; init; }
    public Guid RoomId { get; init; }
    public string EncryptedKey { get; init; }

    public EncryptedSymmetricKey() { }

    public EncryptedSymmetricKey(Guid userId, string encryptedKey, Guid roomId)
    {
        KeyId = Guid.NewGuid();
        UserId = userId;
        RoomId = roomId;
        EncryptedKey = encryptedKey;
    }
}