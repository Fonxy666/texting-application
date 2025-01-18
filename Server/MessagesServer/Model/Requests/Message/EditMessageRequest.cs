using System.ComponentModel.DataAnnotations;

namespace MessagesServer.Model.Requests.Message;

public record EditMessageRequest(
    [Required(ErrorMessage = "Message id cannot be null.")]Guid Id,
    [Required(ErrorMessage = "Message cannot be null.")]string Message,
    [Required(ErrorMessage = "Message cannot be null.")]string Iv);