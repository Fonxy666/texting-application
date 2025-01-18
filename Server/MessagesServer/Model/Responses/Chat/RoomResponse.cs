namespace MessagesServer.Model.Responses.Chat;

public record RoomResponse(bool Success, string RoomId, string RoomName) { }