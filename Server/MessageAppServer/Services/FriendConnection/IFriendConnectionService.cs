using Server.Model.Requests.User;

namespace Server.Services.FriendConnection;

public interface IFriendConnectionService
{
    Task SendFriendRequest(FriendRequest request);
}