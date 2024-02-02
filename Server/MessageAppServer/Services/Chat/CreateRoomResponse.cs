namespace Server.Contracts;

public record CreateRoomResponse(bool Success, string RoomId, string RoomName) { }