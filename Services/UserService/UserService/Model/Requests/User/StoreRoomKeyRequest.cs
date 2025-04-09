namespace UserService.Model.Requests.User;

public record StoreRoomKeyRequest(string EncryptedKey, string RoomId);
