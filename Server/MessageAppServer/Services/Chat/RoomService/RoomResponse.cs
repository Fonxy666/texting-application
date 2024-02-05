namespace Server.Services.Chat;

public record RoomResponse(bool Success, string RoomId, string RoomName) { }