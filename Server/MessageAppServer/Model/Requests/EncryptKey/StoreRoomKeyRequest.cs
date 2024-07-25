namespace Server.Model.Requests.EncryptKey;

public record StoreRoomKeyRequest(string EncryptedKey, string RoomId);