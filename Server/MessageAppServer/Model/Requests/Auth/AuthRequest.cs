using System.ComponentModel.DataAnnotations;

namespace Server.Model.Requests.Auth;

public record AuthRequest(
    [Required(ErrorMessage = "Username cannot be null.")] string UserName,
    [Required(ErrorMessage = "Password cannot be null.")] string Password);
