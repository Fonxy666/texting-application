using System.ComponentModel.DataAnnotations;

namespace UserService.Model.Requests.User;

public record ChangeEmailRequest(
    [Required(ErrorMessage = "Old e-mail cannot be null.")]
    [EmailAddress(ErrorMessage = "The provided string is not an e-mail.")]string OldEmail,
    [Required(ErrorMessage = "New e-mail cannot be null.")]
    [EmailAddress(ErrorMessage = "The provided string is not an e-mail.")]string NewEmail);