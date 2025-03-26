using System.ComponentModel.DataAnnotations;

namespace UserService.Model.Requests.User;

public record PasswordResetRequest(
    [Required(ErrorMessage = "E-mail cannot be null.")]string Email,
    [Required(ErrorMessage = "New password cannot be null.")]string NewPassword);