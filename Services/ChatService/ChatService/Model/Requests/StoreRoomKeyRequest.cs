using System.ComponentModel.DataAnnotations;

namespace ChatService.Model.Requests;

public record StoreRoomKeyRequest(
    [Required(ErrorMessage = "User id cannot be null.")]Guid UserId,
    [Required(ErrorMessage = "Encrypted key cannot be null.")]string EncryptedKey,
    [Required(ErrorMessage = "Room id cannot be null.")]Guid RoomId
);

public record KeyRequest(
    [Required(ErrorMessage = "Room id cannot be null.")]Guid RoomId,
    [Required(ErrorMessage = "Connection id cannot be null.")]Guid ConnectionId,
    [Required(ErrorMessage = "Room name cannot be null.")]string RoomName
);

public record GetSymmetricKeyRequest(
    [Required(ErrorMessage = "Encrypted room key cannot be null.")]string EncryptedRoomKey,
    [Required(ErrorMessage = "Connection id cannot be null.")]Guid ConnectionId,
    [Required(ErrorMessage = "Room id cannot be null.")]Guid RoomId,
    [Required(ErrorMessage = "Room name cannot be null.")]string RoomName
);