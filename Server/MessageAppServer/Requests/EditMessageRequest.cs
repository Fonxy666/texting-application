using System.ComponentModel.DataAnnotations;

namespace Server.Requests;

public record EditMessageRequest([Required]string Id, [Required]string RoomId, [Required]string UserName, [Required]string Message);