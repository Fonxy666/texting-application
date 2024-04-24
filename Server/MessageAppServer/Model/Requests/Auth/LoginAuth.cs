using System.ComponentModel.DataAnnotations;

namespace Server.Model.Requests.Auth;

public record LoginAuth(
    [Required(ErrorMessage = "Username cannot be null.")]string UserName,
    [Required(ErrorMessage = "You need to provide a 'rememberme' boolean.")]bool RememberMe,
    [Required(ErrorMessage = "Token cannot be null.")]string Token);