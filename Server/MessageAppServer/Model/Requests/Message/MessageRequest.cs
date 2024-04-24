using System.ComponentModel.DataAnnotations;

namespace Server.Model.Requests.Message;

public record MessageRequest([Required]string RoomId, [Required]string UserId, [Required]string Message, [Required]bool AsAnonymous, string? MessageId = null);