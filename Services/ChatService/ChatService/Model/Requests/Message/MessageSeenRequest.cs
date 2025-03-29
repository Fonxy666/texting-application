using System.ComponentModel.DataAnnotations;

namespace ChatService.Model.Requests.Message;

public record MessageSeenRequest(
    [Required(ErrorMessage = "UserId cannot be null.")]string UserId);