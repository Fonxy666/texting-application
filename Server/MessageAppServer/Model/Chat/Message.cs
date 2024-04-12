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
    public bool SentAsAnonymous { get; set; }
    public List<Guid> Seen { get; set; }
    
    public Message() { }

    public Message(string roomId, string senderId, string text, bool sentAsAnonymous)
    {
        RoomId = roomId;
        SenderId = senderId;
        Text = text;
        SentAsAnonymous = sentAsAnonymous;
        Seen = new List<Guid>();
        
        Guid senderGuid;
        if (Guid.TryParse(senderId, out senderGuid))
        {
            Seen.Add(senderGuid);
        }
    }
    
    public Message(string roomId, string senderId, string text, string messageId, bool sentAsAnonymous)
    {
        MessageId = messageId;
        RoomId = roomId;
        SenderId = senderId;
        Text = text;
        SentAsAnonymous = sentAsAnonymous;
        Seen = new List<Guid>();
        
        Guid senderGuid;
        if (Guid.TryParse(senderId, out senderGuid))
        {
            Seen.Add(senderGuid);
        }
    }

    public void AddUserToSeen(Guid userId)
    {
        Seen.Add(userId);
    }
}