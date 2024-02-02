using System.ComponentModel.DataAnnotations;
using Server.Model.Chat;

namespace Server.Contracts;

public class CreateRoomRequest([Required] string RoomName, [Required]string Password);