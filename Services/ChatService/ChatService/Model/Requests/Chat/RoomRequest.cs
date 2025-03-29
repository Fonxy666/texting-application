using System.ComponentModel.DataAnnotations;

namespace ChatService.Model.Requests.Chat;

public record RoomRequest(
    [Required(ErrorMessage = "Room name cannot be null.")]string RoomName,
    [Required(ErrorMessage = "Password cannot be null.")]string Password,
    string EncryptedSymmetricRoomKey
    );