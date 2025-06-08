using System.ComponentModel.DataAnnotations;

namespace ChatService.Model.Requests;

public record EditMessageRequest(
    [Required(ErrorMessage = "Message id cannot be null.")]Guid Id,
    [Required(ErrorMessage = "Message cannot be null.")]string Message,
    [Required(ErrorMessage = "Message cannot be null.")]string Iv
);

public record MessageRequest(
    [Required(ErrorMessage = "Room id cannot be null.")]Guid RoomId,
    [Required(ErrorMessage = "Message cannot be null.")]string Message,
    [Required(ErrorMessage = "'AsAnonym' bool cannot be null.")]bool AsAnonymous,
    [Required(ErrorMessage = "Iv cannot be null.")]string Iv,
    string? MessageId
);

public record EditMessageSeenRequest(
    [Required(ErrorMessage = "Message id cannot be null.")]Guid MessageId
);

public record MessageSeenRequest(
    [Required(ErrorMessage = "UserId cannot be null.")]Guid UserId
);
