namespace ChatService.Model.Responses.Chat;

public record KeyRequestResponse(string PublicKey, Guid UserId, string RoomId, string ConnectionId, string RoomName);
public record RoomNameTakenResponse(bool Result);
public record RoomResponseDto(Guid RoomId, string RoomName);