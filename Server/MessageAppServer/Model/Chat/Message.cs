using System.ComponentModel.DataAnnotations;
using System.Globalization;

namespace Server.Model.Chat;

public class Message
{
    [Key]
    public string MessageId { get; set; } = Guid.NewGuid().ToString();

    public string RoomId { get; set; } 
    public string SenderId { get; set; } 
    public string Text { get; set; } 
    public string SendTime { get; set; } = DateTime.Now.ToString(CultureInfo.InvariantCulture);
    
    public Message() { }

    public Message(string roomId, string senderId, string text)
    {
        RoomId = roomId;
        SenderId = senderId;
        Text = text;
    }
    
    public Message(string roomId, string senderId, string text, string messageId)
    {
        RoomId = roomId;
        SenderId = senderId;
        Text = text;
        MessageId = messageId;
    }
}