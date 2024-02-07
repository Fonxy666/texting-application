using System.ComponentModel.DataAnnotations;
using System.Globalization;

namespace Server.Model.Chat;

public class Message
{
    [Key]
    public string MessageId { get; set; } = Guid.NewGuid().ToString();

    public string RoomId { get; set; } 
    public string SenderName { get; set; } 
    public string Text { get; set; } 
    public string SendTime { get; set; } = DateTime.Now.ToString(CultureInfo.InvariantCulture);
    
    public Message() { }

    public Message(string roomId, string senderName, string text)
    {
        RoomId = roomId;
        SenderName = senderName;
        Text = text;
    }
}