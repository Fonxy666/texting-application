using System.ComponentModel.DataAnnotations;

namespace Server.Requests.Auth;

public record AuthRequest([Required]string UserName, [Required]string Password);