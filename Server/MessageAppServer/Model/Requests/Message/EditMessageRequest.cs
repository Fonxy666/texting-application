using System.ComponentModel.DataAnnotations;

namespace Server.Model.Requests.Message;

public record EditMessageRequest([Required]string Id, [Required]string RoomId, [Required]string UserName, [Required]string Message);