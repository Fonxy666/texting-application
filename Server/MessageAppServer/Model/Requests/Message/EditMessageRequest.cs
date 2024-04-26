using System.ComponentModel.DataAnnotations;

namespace Server.Model.Requests.Message;

public record EditMessageRequest(
    [Required(ErrorMessage = "Message id cannot be null.")]string Id,
    [Required(ErrorMessage = "Message cannot be null.")]string Message);