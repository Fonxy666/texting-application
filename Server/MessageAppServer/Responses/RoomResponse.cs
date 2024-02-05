namespace Server.Responses;

public record RoomResponse(bool Success, string RoomId, string RoomName) { }