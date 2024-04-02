using System.ComponentModel.DataAnnotations;

namespace Server.Model.Requests.Chat;

public record RoomRequest([Required]string RoomName, [Required]string Password);