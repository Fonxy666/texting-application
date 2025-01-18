using System.ComponentModel.DataAnnotations;

namespace MessagesServer.Model.Requests.Chat;

public record JoinRoomRequest(
    [Required(ErrorMessage = "Room name cannot be null.")]string RoomName,
    [Required(ErrorMessage = "Password cannot be null.")]string Password);