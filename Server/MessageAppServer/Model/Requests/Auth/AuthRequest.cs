using System.ComponentModel.DataAnnotations;

namespace Server.Model.Requests.Auth;

public record AuthRequest([Required]string UserName, [Required]string Password);