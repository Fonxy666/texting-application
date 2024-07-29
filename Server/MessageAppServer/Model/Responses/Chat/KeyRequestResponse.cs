namespace Server.Model.Responses.Chat;

public record KeyRequestResponse(string PublicKey, Guid UserId, string RoomId, string ConnectionId, string RoomName);