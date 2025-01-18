namespace AuthenticationServer.Model.Requests.User;

public record FriendRequest(string SenderId, string Receiver);