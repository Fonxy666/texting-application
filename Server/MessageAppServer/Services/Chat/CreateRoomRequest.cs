using System.ComponentModel.DataAnnotations;

namespace Server.Services.Chat;

public record CreateRoomRequest([Required]string RoomName, [Required]string Password);