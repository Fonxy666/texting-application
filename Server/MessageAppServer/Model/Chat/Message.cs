using System.ComponentModel.DataAnnotations;

namespace Server.Model.Chat;

public class Message(Guid roomId, string senderName, string text, DateTime sendTime)
{
    [Key]
    public string MessageId { get; set; } = Guid.NewGuid().ToString();
    
    public Guid RoomId { get; set; } = roomId;
    public string SenderName { get; set; } = senderName;
    public string Text { get; set; } = text;
    public DateTime SendTime { get; set; } = sendTime;
}