using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Text;

namespace Server.Model.Chat;

public class Room
{
    [Key]
    public string RoomId { get; set; } = Guid.NewGuid().ToString();
    
    public string RoomName { get; set; }
    public string Password { get; set; }
    
    public Room() {}
    
    public Room(string roomName, string password)
    {
        RoomName = roomName;
        Password = HashPassword(password);
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
}