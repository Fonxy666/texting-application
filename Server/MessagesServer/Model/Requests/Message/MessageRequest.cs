using System.ComponentModel.DataAnnotations;

namespace MessagesServer.Model.Requests.Message;

public record MessageRequest(
    [Required(ErrorMessage = "Room id cannot be null.")]string RoomId,
    [Required(ErrorMessage = "Message cannot be null.")]string Message,
    [Required(ErrorMessage = "'AsAnonym' bool cannot be null.")]bool AsAnonymous,
    [Required(ErrorMessage = "Iv cannot be null.")]string Iv,
    string? MessageId);