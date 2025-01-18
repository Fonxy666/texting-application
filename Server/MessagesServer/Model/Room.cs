using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Security.Cryptography;
using System.Text;

namespace MessagesServer.Model.Chat;

public class Room
{
    [Key]
    public Guid RoomId { get; private set; }
    public Guid CreatorId { get; init; }
    [ForeignKey("CreatorId")]
    public ApplicationUser CreatorUser { get; set; }
    public string RoomName { get; init; }
    public string Password { get; private set; }
    public ICollection<Message> Messages { get; set; } = new List<Message>();
    public ICollection<EncryptedSymmetricKey> EncryptedSymmetricKeys { get; private set; } = new List<EncryptedSymmetricKey>();
    
    public Room() {}
    
    public Room(string roomName, string password, Guid creatorId)
    {
        RoomId = Guid.NewGuid();
        CreatorId = creatorId;
        RoomName = roomName;
        Password = HashPassword(password);
    }

    public void SetRoomIdForTests(string id)
    {
        RoomId = new Guid(id);
    }

    private static string HashPassword(string password)
    {
        using var sha256 = SHA256.Create();
        var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            
        return BitConverter.ToString(hashedBytes).Replace("-", "").ToLower();
    }

    public bool PasswordMatch(string enteredPassword)
    {
        var enteredPasswordHash = HashPassword(enteredPassword);
        return Password == enteredPasswordHash;
    }

    public void ChangePassword(string password)
    {
        Password = HashPassword(password);
    }

    public bool IsCreator(Guid userId)
    {
        return CreatorId == userId;
    }

    public void AddNewSymmetricKey(Guid userId, string key)
    {
        EncryptedSymmetricKeys.Add(new EncryptedSymmetricKey(userId, key, RoomId));
    }
}