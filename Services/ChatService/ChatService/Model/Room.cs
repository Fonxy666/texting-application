using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Text;

namespace ChatService.Model;

public class Room
{
    [Key]
    public Guid RoomId { get; private set; }
    public Guid CreatorId { get; init; }
    public string RoomName { get; init; }
    public string Password { get; private set; }
    public ICollection<Message> Messages { get; private set; } = new List<Message>();
    
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
}