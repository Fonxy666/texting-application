namespace ChatService.Model.Responses.Chat;

public record KeyRequestResponse(string PublicKey, Guid UserId, Guid RoomId, string ConnectionId, string RoomName);
public record RoomResponseDto(Guid RoomId, string RoomName);
public record ReceiveMessageResponse(string User, string Message, DateTime MessageTime, Guid? UserId, Guid? MessageId, List<Guid>? SeenList, Guid RoomId, string? Iv);
public record ReceiveMessageResponseForBot(string User, string Message, DateTime MessageTime, Guid RoomId);
public record EditMessageResponse(Guid RequestId, string Message);
public record SendKeyResponse(string EncryptedRoomKey, Guid RoomId, string RoomName);