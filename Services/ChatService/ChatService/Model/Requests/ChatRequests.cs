using System.ComponentModel.DataAnnotations;

namespace ChatService.Model.Requests;

public record ChangeRoomPassword(
[Required(ErrorMessage = "Room id cannot be null.")]Guid Id,
[Required(ErrorMessage = "Old password cannot be null.")]string OldPassword,
[Required(ErrorMessage = "Password cannot be null.")]string Password);

public record JoinRoomRequest(
    [Required(ErrorMessage = "Room name cannot be null.")]string RoomName,
    [Required(ErrorMessage = "Password cannot be null.")]string Password);

public record RoomRequest(
    [Required(ErrorMessage = "Room name cannot be null.")]string RoomName,
    [Required(ErrorMessage = "Password cannot be null.")]string Password,
    string EncryptedSymmetricRoomKey
);

public record GetMessagesRequest(
    [Required(ErrorMessage = "Room id cannot be null.")]Guid RoomId,
    [Required(ErrorMessage = "Index cannot be null.")]int Index
);
