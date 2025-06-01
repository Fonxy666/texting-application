namespace ChatService.Model.Requests;

public record StoreRoomKeyRequest(Guid UserId, string EncryptedKey, Guid RoomId);