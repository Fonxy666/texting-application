using System.ComponentModel.DataAnnotations;

namespace Server.Model.Requests.Chat;

public record ChangePasswordRequest(
    [Required(ErrorMessage = "Room id cannot be null.")]string RoomId,
    [Required(ErrorMessage = "Password cannot be null.")]string OldPassword,
    [Required(ErrorMessage = "Password cannot be null.")]string NewPassword);