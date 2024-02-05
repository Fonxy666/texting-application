using System.ComponentModel.DataAnnotations;

namespace Server.Services.Chat.MessageService;

public record MessageRequest([Required]string RoomId, [Required]string UserName, [Required]string Message);