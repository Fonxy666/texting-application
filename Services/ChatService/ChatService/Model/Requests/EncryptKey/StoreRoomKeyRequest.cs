namespace ChatService.Model.Requests.EncryptKey;

public record StoreRoomKeyRequest(Guid UserId, string EncryptedKey, Guid RoomId);