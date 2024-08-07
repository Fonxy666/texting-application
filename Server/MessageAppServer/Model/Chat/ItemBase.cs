using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Globalization;

namespace Server.Model.Chat;

public abstract class ItemBase
{
    [Key]
    public  Guid ItemId { get; set; }
    public Guid RoomId { get; init; }
    [ForeignKey("RoomId")]
    public Room Room { get; set; }
    public Guid SenderId { get; init; }
    public string SendTime { get; init; }
    public bool SentAsAnonymous { get; init; }
    public string Iv { get; set; }
    public List<Guid> Seen { get; set; }

    protected ItemBase() { }

    protected ItemBase(Guid roomId, Guid senderId, bool sentAsAnonymous, string iv)
    {
        ItemId = Guid.NewGuid(); 
        RoomId = roomId;
        SenderId = senderId;
        SentAsAnonymous = sentAsAnonymous;
        Iv = iv;
        Seen = new List<Guid> { SenderId };
        SendTime = DateTime.Now.ToString(CultureInfo.InvariantCulture);
    }

    protected ItemBase(Guid roomId, Guid senderId, Guid messageId, bool sentAsAnonymous, string iv)
    {
        ItemId = messageId;
        RoomId = roomId;
        SenderId = senderId;
        SentAsAnonymous = sentAsAnonymous;
        Iv = iv;
        Seen = new List<Guid> { SenderId };
        SendTime = DateTime.Now.ToString(CultureInfo.InvariantCulture);
    }

    public void AddUserToSeen(Guid userId)
    {
        Seen.Add(userId);
    }

    public void ChangeMessageIv(string iv)
    {
        Iv = iv;
    }
}