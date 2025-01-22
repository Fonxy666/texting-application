using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Globalization;

namespace MessagesServer.Model;

public class Message
{
    [Key]
    public Guid MessageId { get; private set; }
    public Guid RoomId { get; init; }
    [ForeignKey("RoomId")]
    public Room Room { get; set; }
    public Guid SenderId { get; init; }
    public string Text { get; private set; }
    public string SendTime { get; init; } = DateTime.Now.ToString(CultureInfo.InvariantCulture);
    public bool SentAsAnonymous { get; init; }
    public string Iv { get; set; }
    public List<Guid> Seen { get; set; }
    
    public Message() { }

    public Message(Guid roomId, Guid senderId, string text, bool sentAsAnonymous, string iv)
    {
        MessageId = Guid.NewGuid(); 
        RoomId = roomId;
        SenderId = senderId;
        Text = text;
        SentAsAnonymous = sentAsAnonymous;
        Iv = iv;
        Seen = new List<Guid> { SenderId };
    }
    
    public Message(Guid roomId, Guid senderId, string text, Guid messageId, bool sentAsAnonymous, string iv)
    {
        MessageId = messageId;
        RoomId = roomId;
        SenderId = senderId;
        Text = text;
        SentAsAnonymous = sentAsAnonymous;
        Iv = iv;
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

    public void ChangeMessageIv(string iv)
    {
        Iv = iv;
    }
}