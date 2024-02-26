using System.ComponentModel.DataAnnotations;

namespace Server.Requests.Chat;

public record RoomRequest([Required]string RoomName, [Required]string Password);