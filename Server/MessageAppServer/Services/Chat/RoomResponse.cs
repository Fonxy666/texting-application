namespace Server.Contracts;

public record RoomResponse(bool Success, string RoomId, string RoomName) { }