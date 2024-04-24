using System.ComponentModel.DataAnnotations;

namespace Server.Model.Requests.User;

public record ChangeUserPasswordRequest(
    [Required(ErrorMessage = "User id cannot be null.")]string Id,
    [Required(ErrorMessage = "Old password cannot be null.")]string OldPassword,
    [Required(ErrorMessage = "Password cannot be null.")]
    [MinLength(8, ErrorMessage = "Password must be at least 8 characters long.")]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*\W).+$",
        ErrorMessage = "Password must contain at least one lowercase letter, one uppercase letter, one digit, and one special character.")]
    string Password,
    [Required(ErrorMessage = "Password cannot be null.")]
    [MinLength(8, ErrorMessage = "Password must be at least 8 characters long.")]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*\W).+$",
        ErrorMessage = "Password must contain at least one lowercase letter, one uppercase letter, one digit, and one special character.")]
    string PasswordRepeat);