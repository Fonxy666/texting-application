using System.ComponentModel.DataAnnotations;

namespace Server.Requests.Message;

public record MessageRequest([Required]string RoomId, [Required]string UserName, [Required]string Message, string? MessageId = null);