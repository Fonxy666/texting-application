using System.ComponentModel.DataAnnotations;

namespace Server.Requests;

public record RoomRequest([Required]string RoomName, [Required]string Password);