using System.ComponentModel.DataAnnotations;

namespace Server.Model.Requests.Message;

public record MessageRequest(
    [Required(ErrorMessage = "Room id cannot be null.")]string RoomId,
    [Required(ErrorMessage = "User id cannot be null.")]string UserId,
    [Required(ErrorMessage = "Message cannot be null.")]string Message,
    [Required(ErrorMessage = "'AsAnonym' bool cannot be null.")]bool AsAnonymous,
    string? MessageId = null);