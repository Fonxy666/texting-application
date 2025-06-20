namespace ChatService.Model.Responses.Chat;

public record KeyRequestResponse(string PublicKey, Guid UserId, Guid RoomId, string ConnectionId, string RoomName);
public record RoomResponseDto(Guid RoomId, string RoomName);
public record ReceiveMessageResponse(string UserName, string Text, DateTime SendTime, Guid SenderId, Guid? MessageId, List<Guid>? SeenList, Guid RoomId, string? Iv);
public record ReceiveMessageResponseForBot(string SenderId, string Text, DateTime SendTime, Guid RoomId);
public record EditMessageResponse(Guid RequestId, string Text);
public record SendKeyResponse(string EncryptedRoomKey, Guid RoomId, string RoomName);