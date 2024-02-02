using System.ComponentModel.DataAnnotations;

namespace Server.Services.Chat;

public record RoomRequest([Required]string RoomName, [Required]string Password);