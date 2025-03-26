using System.ComponentModel.DataAnnotations;

namespace UserService.Model.Requests.Auth;

public record VerifyTokenRequest(
    [Required(ErrorMessage = "E-mail cannot be null.")] string Email,
    [Required(ErrorMessage = "Token cannot be null.")] string VerifyCode);