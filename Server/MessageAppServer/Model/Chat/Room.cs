using System.ComponentModel.DataAnnotations;

namespace Server.Model.Chat;

public class Room(string roomName)
{
    [Key]
    public string RoomId { get; set; } = Guid.NewGuid().ToString();
    
    public string RoomName { get; set; } = roomName;
}