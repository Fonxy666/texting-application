using System.ComponentModel.DataAnnotations;

namespace Server.Model.Requests.Chat;

public record ChangeRoomPassword(
    [Required(ErrorMessage = "User id cannot be null.")]string Id,
    [Required(ErrorMessage = "Old password cannot be null.")]string OldPassword,
    [Required(ErrorMessage = "Password cannot be null.")]string Password);