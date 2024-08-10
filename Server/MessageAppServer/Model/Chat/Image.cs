namespace Server.Model.Chat;

public class Image : ItemBase
{
    public string ImagePath { get; set; }

    public Image() { }

    public Image(Guid roomId, Guid senderId, string imagePath, bool sentAsAnonymous, string iv)
        : base(roomId, senderId, sentAsAnonymous, iv)
    {
        ImagePath = imagePath;
    }
    
    public Image(Guid roomId, Guid senderId, string imagePath, Guid messageId, bool sentAsAnonymous, string iv)
        : base(roomId, senderId, messageId, sentAsAnonymous, iv)
    {
        ImagePath = imagePath;
    }
}