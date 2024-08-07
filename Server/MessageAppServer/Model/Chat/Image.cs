using System.ComponentModel.DataAnnotations;

namespace Server.Model.Chat;

public class Image : ItemBase
{
    [Key]
    public new Guid ItemId { get; set; }
    public string ImagePath { get; set; }

    public Image() { }

    public Image(Guid roomId, Guid senderId, string imagePath, bool sentAsAnonymous, string iv)
        : base(roomId, senderId, sentAsAnonymous, iv)
    {
        ItemId = Guid.NewGuid();
        ImagePath = imagePath;
    }
    
    public Image(Guid roomId, Guid senderId, string imagePath, Guid messageId, bool sentAsAnonymous, string iv)
        : base(roomId, senderId, messageId, sentAsAnonymous, iv)
    {
        ItemId = messageId;
        ImagePath = imagePath;
    }
}