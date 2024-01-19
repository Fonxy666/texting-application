using System.ComponentModel.DataAnnotations;

namespace Server.Services.Authentication;

public record AuthRequest([Required]string UserName, [Required]string Password);