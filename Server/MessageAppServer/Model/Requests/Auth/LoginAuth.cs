using System.ComponentModel.DataAnnotations;

namespace Server.Model.Requests.Auth;

public record LoginAuth([Required]string UserName, [Required]bool RememberMe, [Required]string Token);