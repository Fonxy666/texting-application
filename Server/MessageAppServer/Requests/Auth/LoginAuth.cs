using System.ComponentModel.DataAnnotations;

namespace Server.Requests.Auth;

public record LoginAuth([Required]string UserName, [Required]string Password, [Required]bool RememberMe, [Required]string Token);