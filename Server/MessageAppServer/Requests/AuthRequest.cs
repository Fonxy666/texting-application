using System.ComponentModel.DataAnnotations;

namespace Server.Requests;

public record AuthRequest([Required]string UserName, [Required]string Password, [Required]bool RememberMe);