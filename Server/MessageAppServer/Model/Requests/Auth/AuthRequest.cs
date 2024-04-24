using System.ComponentModel.DataAnnotations;

namespace Server.Model.Requests.Auth;

public record AuthRequest(
    [Required(ErrorMessage = "Username cannot be null.")] string UserName,
    [Required(ErrorMessage = "Password cannot be null.")]
    [MinLength(8, ErrorMessage = "Password must be at least 8 characters long.")]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*\W).+$",
        ErrorMessage = "Password must contain at least one lowercase letter, one uppercase letter, one digit, and one special character.")]
    string Password
);
