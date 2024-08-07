using System.ComponentModel.DataAnnotations;

namespace Server.Model.Chat;

public class Message : ItemBase
{
    [Key]
    public new Guid ItemId { get; set; }
    public string Text { get; set; }

    public Message() { }

    public Message(Guid roomId, Guid senderId, string text, bool sentAsAnonymous, string iv)
        : base(roomId, senderId, sentAsAnonymous, iv)
    {
        ItemId = Guid.NewGuid();
        Text = text;
    }
    
    public Message(Guid roomId, Guid senderId, string text, Guid messageId, bool sentAsAnonymous, string iv)
        : base(roomId, senderId, messageId, sentAsAnonymous, iv)
    {
        ItemId = messageId;
        Text = text;
    }
    
    public void ChangeMessageText(string newText)
    {
        Text = newText;
    }
}