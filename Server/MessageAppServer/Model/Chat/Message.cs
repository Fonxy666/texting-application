namespace Server.Model.Chat;

public class Message : ItemBase
{
    public string Text { get; set; }

    public Message() { }

    public Message(Guid roomId, Guid senderId, string text, bool sentAsAnonymous, string iv)
        : base(roomId, senderId, sentAsAnonymous, iv)
    {
        Text = text;
    }
    
    public Message(Guid roomId, Guid senderId, string text, Guid messageId, bool sentAsAnonymous, string iv)
        : base(roomId, senderId, messageId, sentAsAnonymous, iv)
    {
        Text = text;
    }
    
    public void ChangeMessageText(string newText)
    {
        Text = newText;
    }
}