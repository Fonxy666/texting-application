using System.ComponentModel.DataAnnotations;
using System.Globalization;

namespace Server.Model.Chat;

public class Message(string roomId, string senderName, string text)
{
    [Key]
    public string MessageId { get; set; } = Guid.NewGuid().ToString();

    public string RoomId { get; set; } = roomId;
    public string SenderName { get; set; } = senderName;
    public string Text { get; set; } = text;
    public string SendTime { get; set; } = DateTime.Now.ToString(CultureInfo.InvariantCulture);
}