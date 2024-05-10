using System.ComponentModel.DataAnnotations;
using System.Globalization;

namespace Server.Model.Chat;

public class Message
{
    [Key]
    public Guid MessageId { get; private set; }
    public Guid RoomId { get; init; } 
    public Guid SenderId { get; init; } 
    public string Text { get; private set; } 
    public string SendTime { get; init; } = DateTime.Now.ToString(CultureInfo.InvariantCulture);
    public bool SentAsAnonymous { get; init; }
    public List<Guid> Seen { get; set; }
    
    public Message() { }

    public Message(Guid roomId, Guid senderId, string text, bool sentAsAnonymous)
    {
        MessageId = Guid.NewGuid(); 
        RoomId = roomId;
        SenderId = senderId;
        Text = text;
        SentAsAnonymous = sentAsAnonymous;
        Seen = new List<Guid> { SenderId };
    }
    
    public Message(Guid roomId, Guid senderId, string text, Guid messageId, bool sentAsAnonymous)
    {
        MessageId = messageId;
        RoomId = roomId;
        SenderId = senderId;
        Text = text;
        SentAsAnonymous = sentAsAnonymous;
        Seen = new List<Guid> { SenderId };
    }

    public void AddUserToSeen(Guid userId)
    {
        Seen.Add(userId);
    }

    public void ChangeMessageText(string newText)
    {
        Text = newText;
    }
}