using System.ComponentModel.DataAnnotations;

namespace Server.Requests;

public record MessageRequest([Required]string RoomId, [Required]string UserName, [Required]string Message);