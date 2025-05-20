namespace UserService.Models;

public class FriendConnection
{    public Guid ConnectionId { get; init; } = Guid.NewGuid();

    public Guid SenderId { get; init; }
    public ApplicationUser? Sender { get; set; }

    public Guid ReceiverId { get; init; }
    public ApplicationUser? Receiver { get; set; }

    public FriendStatus Status { get; private set; } = FriendStatus.Pending;

    public DateTime SentTime { get; private set; } = DateTime.UtcNow;

    public DateTime? AcceptedTime { get; private set; }

    public FriendConnection() { }

    public FriendConnection(Guid senderId, Guid receiverId)
    {
        ConnectionId = Guid.NewGuid();
        SenderId = senderId;
        ReceiverId = receiverId;
        Status = FriendStatus.Pending;
        SentTime = DateTime.UtcNow;
        AcceptedTime = null;
    }
    
    public FriendConnection(Guid connectionId, Guid senderId, Guid receiverId, DateTime sentTime, DateTime acceptedTime)
    {
        ConnectionId = connectionId;
        SenderId = senderId;
        ReceiverId = receiverId;
        Status = FriendStatus.Accepted;
        SentTime = sentTime;
        AcceptedTime = acceptedTime;
    }

    public void SetStatusToAccepted()
    {
        Status = FriendStatus.Accepted;
        AcceptedTime = DateTime.UtcNow;
    }
}